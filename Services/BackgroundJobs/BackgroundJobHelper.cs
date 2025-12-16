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
        var pollingTask = CreatePollingTask(jobId, jobProgressRepo, jobManager, pollingIntervalMs);
        var workTask = CreateWorkTask(jobId, scope, jobManager, workAction);

        // Ejecutar en paralelo
        try
        {
            await Task.WhenAll(workTask, pollingTask);
            var result = await workTask;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private Task CreatePollingTask(
                                    string jobId,
                                    IJobProgressRepository jobProgressRepo,
                                    IJobManager jobManager,
                                    int pollingIntervalMs)
    {
        return Task.Run(async () =>
                    {
                        var cts = jobManager.GetCancellationToken(jobId);

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
}
