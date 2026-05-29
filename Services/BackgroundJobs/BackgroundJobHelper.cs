// <copyright file="BackgroundJobHelper.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Helper para ejecutar background jobs con patrón estándar de polling + SP.
/// </summary>
public class BackgroundJobHelper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public BackgroundJobHelper(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta un job con polling automático y manejo de conexión Oracle.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<TResult> ExecuteJobAsync<TResult>(
                                                        string jobId,
                                                        string description,
                                                        Func<OracleConnection,
                                                        CancellationToken,
                                                        Task<TResult>> workAction,
                                                        int pollingIntervalMs = 2000)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobProgressRepo = scope.ServiceProvider.GetRequiredService<IJobProgressRepository>();
        var jobManager = scope.ServiceProvider.GetRequiredService<IJobManager>();

        // Inicializar progreso
        await jobProgressRepo.InitializeAsync(jobId, description);

        // Crear tasks
        using var pollingCts = new CancellationTokenSource();
        var workTask = CreateWorkTask(jobId, scope, jobManager, workAction);
        var pollingTask = CreatePollingTask(jobId, jobProgressRepo, jobManager, pollingIntervalMs, pollingCts.Token);

        // Ejecutar en paralelo
        try
        {
            try
            {
                return await workTask;
            }
            finally
            {
                await pollingCts.CancelAsync();
                try { await pollingTask; } catch (OperationCanceledException) { }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            // ✅ Limpiar registro de progreso temporal
            await CleanupProgressAsync(jobProgressRepo, jobId);
        }
    }

    /// <summary>
    /// Ejecuta un job con logging dual (archivo + BD) usando JobAuditLogger.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<TResult> ExecuteJobWithLoggingAsync<TResult>(
                                                                    string jobId,
                                                                    string description,
                                                                    Func<OracleConnection,
                                                                    JobAuditLogger,
                                                                    CancellationToken,
                                                                    Task<TResult>> workAction,
                                                                    int pollingIntervalMs = 2000)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobProgressRepo = scope.ServiceProvider.GetRequiredService<IJobProgressRepository>();
        var jobAuditRepo = scope.ServiceProvider.GetRequiredService<IJobAuditRepository>();
        var jobManager = scope.ServiceProvider.GetRequiredService<IJobManager>();

        // Crear logger dual
        var jobLogger = new JobAuditLogger(jobId, jobAuditRepo, _logger);

        // Inicializar progreso
        await jobProgressRepo.InitializeAsync(jobId, description);

        // CTS propio del polling: se cancela cuando el workTask termina (con éxito, error o cancelación),
        // evitando que el polling quede bloqueado en jobs que no actualizan la tabla de progreso Oracle.
        using var pollingCts = new CancellationTokenSource();

        // Crear tasks
        var workTask = CreateWorkTaskWithLogger(jobId, scope, jobManager, jobLogger, workAction);
        var pollingTask = CreatePollingTask(jobId, jobProgressRepo, jobManager, pollingIntervalMs, pollingCts.Token);

        // Ejecutar en paralelo
        try
        {
            // Esperamos el trabajo; cuando termine (ok, error o cancelación) detenemos el polling
            try
            {
                return await workTask;
            }
            finally
            {
                await pollingCts.CancelAsync();
                // Esperamos que el polling se detenga limpiamente antes de salir
                try { await pollingTask; } catch (OperationCanceledException) { }
            }
        }
        catch (OperationCanceledException)
        {
            await jobLogger.LogWarningAsync("Job cancelado por el usuario");
            throw;
        }
        catch (Exception ex)
        {
            await jobLogger.LogErrorAsync("Job falló con error", ex);
            throw;
        }
        finally
        {
            // ✅ Limpiar registro de progreso temporal
            await CleanupProgressAsync(jobProgressRepo, jobId);
        }
    }

    /// <summary>
    /// Limpia el registro temporal de progreso del job.
    /// </summary>
    private async Task CleanupProgressAsync(IJobProgressRepository jobProgressRepo, string jobId)
    {
        try
        {
            await jobProgressRepo.DeleteAsync(jobId);
            _logger.LogDebug("Registro de progreso limpiado para job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al limpiar registro de progreso para job {JobId}", jobId);
        }
    }

    private Task CreatePollingTask(
                                    string jobId,
                                    IJobProgressRepository jobProgressRepo,
                                    IJobManager jobManager,
                                    int pollingIntervalMs,
                                    CancellationToken externalCancellationToken = default)
    {
        return Task.Run(async () =>
     {
         // Combinar el token del job (cancelación manual) con el token externo (work finalizado)
         var jobToken = jobManager.GetCancellationToken(jobId);
         using var linked = CancellationTokenSource.CreateLinkedTokenSource(jobToken, externalCancellationToken);
         var cts = linked.Token;

         while (!cts.IsCancellationRequested)
         {
             try
             {
                 var progressData = await jobProgressRepo.GetProgressAsync(jobId);
                 jobManager.UpdateProgress(jobId, progressData.progressPct, progressData.statusMessage);

                 if (progressData.progressPct >= 100 || progressData.progressPct < 0)
                 {
                     break;
                 }
             }
             catch (Exception ex)
             {
                 if (cts.IsCancellationRequested)
                 {
                     break;
                 }

                 _logger.LogError(ex, "Error polling progress for job {JobId}", jobId);
             }

             await Task.Delay(pollingIntervalMs, cts);
         }
     });
    }

    private Task<TResult> CreateWorkTask<TResult>(
                                                    string jobId,
                                                    IServiceScope scope,
                                                    IJobManager jobManager,
                                                    Func<OracleConnection,
                                                    CancellationToken,
                                                    Task<TResult>> workAction)
    {
        return Task.Run(async () =>
        {
            var cts = jobManager.GetCancellationToken(jobId);
            OracleConnection? oracleConnection = null;

            try
            {
                var connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>()["OracleConfig:ConnectionString"]
              ?? throw new InvalidOperationException("Oracle connection string not configured");

                oracleConnection = new OracleConnection(connectionString);
                await oracleConnection.OpenAsync(cts);
                jobManager.RegisterOracleConnection(jobId, oracleConnection);

                // Ejecutar trabajo
                return await workAction(oracleConnection, cts);
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Job cancelado", ex);
                }

                throw;
            }
            finally
            {
                jobManager.UnregisterOracleConnection(jobId);

                if (oracleConnection != null)
                {
                    try
                    {
                        if (oracleConnection.State == ConnectionState.Open)
                        {
                            oracleConnection.Close();
                        }

                        oracleConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al cerrar conexión para job {JobId}", jobId);
                    }
                }
            }
        });
    }

    private Task<TResult> CreateWorkTaskWithLogger<TResult>(
                                                            string jobId,
                                                            IServiceScope scope,
                                                            IJobManager jobManager,
                                                            JobAuditLogger jobLogger,
                                                            Func<OracleConnection,
                                                            JobAuditLogger,
                                                            CancellationToken,
 Task<TResult>> workAction)
    {
        return Task.Run(async () =>
        {
            var cts = jobManager.GetCancellationToken(jobId);
            OracleConnection? oracleConnection = null;

            try
            {
                var connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>()["OracleConfig:ConnectionString"]
         ?? throw new InvalidOperationException("Oracle connection string not configured");

                oracleConnection = new OracleConnection(connectionString);
                await oracleConnection.OpenAsync(cts);
                jobManager.RegisterOracleConnection(jobId, oracleConnection);

                // Ejecutar trabajo con logger
                return await workAction(oracleConnection, jobLogger, cts);
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Job cancelado", ex);
                }

                throw;
            }
            finally
            {
                jobManager.UnregisterOracleConnection(jobId);

                if (oracleConnection != null)
                {
                    try
                    {
                        if (oracleConnection.State == ConnectionState.Open)
                        {
                            oracleConnection.Close();
                        }

                        oracleConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al cerrar conexión para job {JobId}", jobId);
                    }
                }
            }
        });
    }
}
