// <copyright file="BackgroundJobExecutor.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Text.Json;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Services;

public class BackgroundJobExecutor : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<BackgroundJobExecutor> logger;
    private readonly IConfiguration configuration;
    private readonly ConcurrentQueue<string> jobQueue;
    private readonly SemaphoreSlim signal;

    public BackgroundJobExecutor(IServiceProvider serviceProvider, ILogger<BackgroundJobExecutor> logger, IConfiguration configuration)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.configuration = configuration;
        jobQueue = new ConcurrentQueue<string>();
        signal = new SemaphoreSlim(0);
    }

    // Exponer el ServiceProvider para que el controller pueda usarlo
    public IServiceProvider ServiceProvider => serviceProvider;

    public void EnqueueJob(string jobId)
    {
        jobQueue.Enqueue(jobId);
        signal.Release();
        logger.LogInformation("Job {JobId} encolado para ejecución", jobId);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundJobExecutor iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Esperar señal de que hay un job disponible
                await signal.WaitAsync(stoppingToken);

                if (jobQueue.TryDequeue(out var jobId))
                {
                    logger.LogInformation("Procesando job {JobId}", jobId);

                    // Ejecutar en un scope nuevo para obtener servicios scoped
                    _ = Task.Run(
                        async () =>
                    {
                        using var scope = serviceProvider.CreateScope();
                        var jobManager = scope.ServiceProvider.GetRequiredService<IJobManager>();
                        var jobAuditRepo = scope.ServiceProvider.GetRequiredService<IJobAuditRepository>();

                        try
                        {
                            var jobAction = jobManager.GetJobAction(jobId);
                            if (jobAction == null)
                            {
                                logger.LogWarning("Job {JobId} no tiene acción asociada", jobId);
                                return;
                            }

                            // Marcar como iniciado en JobManager
                            jobManager.MarkJobAsStarted(jobId);

                            // Marcar como iniciado en auditoría de Oracle
                            try
                            {
                                await jobAuditRepo.MarkJobAsStartedAsync(jobId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Error al marcar job como iniciado en auditoría");
                            }

                            var progress = new Progress<int>(percentage =>
                            {
                                jobManager.UpdateProgress(jobId, percentage);
                            });

                            var cancellationToken = jobManager.GetCancellationToken(jobId);

                            // Ejecutar el job con timeout configurable (default 60 minutos, 0 = sin timeout)
                            var timeoutMinutes = configuration.GetValue<int>("BackgroundJobs:TimeoutMinutes", 60);

                            CancellationToken executionToken;
                            CancellationTokenSource? timeoutCts = null;
                            CancellationTokenSource? linkedCts = null;

                            if (timeoutMinutes > 0)
                            {
                                // Con timeout configurado
                                timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
                                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                    cancellationToken, timeoutCts.Token);
                                executionToken = linkedCts.Token;

                                logger.LogInformation(
                                    "Job {JobId} ejecutándose con timeout de {Timeout} minutos",
                                    jobId, timeoutMinutes);
                            }
                            else
                            {
                                // Sin timeout (solo cancelación manual)
                                executionToken = cancellationToken;
                                logger.LogWarning("Job {JobId} ejecutándose SIN timeout automático", jobId);
                            }

                            try
                            {
                                var result = await jobAction(progress, executionToken);
                                jobManager.CompleteJob(jobId, result);

                                // Guardar en auditoría de Oracle
                                try
                                {
                                    var jobInfo = jobManager.GetJobInfo(jobId);
                                    var resultJson = result != null ? JsonSerializer.Serialize(result) : null;

                                    await jobAuditRepo.CompleteJobAuditAsync(
                                        jobId,
                                        "COMPLETED",
                                        jobInfo?.ProgressPercentage ?? 100,
                                        resultJson);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, "Error al guardar resultado en auditoría");
                                }
                            }
                            finally
                            {
                                timeoutCts?.Dispose();
                                linkedCts?.Dispose();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Verificar si fue cancelación del usuario o timeout
                            var jobInfo = jobManager.GetJobInfo(jobId);
                            if (jobInfo?.Status == JobStatus.Cancelled)
                            {
                                // Ya fue marcado como cancelado por CancelJob - no hacer nada más
                                logger.LogWarning("Job {JobId} fue cancelado por el usuario", jobId);
                            }
                            else
                            {
                                // Fue timeout - marcar como fallido
                                var errorMsg = "Job cancelado o timeout excedido";
                                jobManager.FailJob(jobId, errorMsg);

                                // Guardar en auditoría
                                try
                                {
                                    await jobAuditRepo.CompleteJobAuditAsync(
                                        jobId,
                                        "TIMEOUT",
                                        0,
                                        null,
                                        errorMsg);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, "Error al guardar timeout en auditoría");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error ejecutando job {JobId}", jobId);
                            jobManager.FailJob(jobId, $"Error: {ex.Message}");

                            // Guardar en auditoría
                            try
                            {
                                await jobAuditRepo.CompleteJobAuditAsync(
                                    jobId,
                                    "FAILED",
                                    0,
                                    null,
                                    ex.Message);
                            }
                            catch (Exception auditEx)
                            {
                                logger.LogWarning(auditEx, "Error al guardar error en auditoría");
                            }
                        }
                    }, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown normal
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en BackgroundJobExecutor");
            }
        }

        logger.LogInformation("BackgroundJobExecutor detenido");
    }
}
