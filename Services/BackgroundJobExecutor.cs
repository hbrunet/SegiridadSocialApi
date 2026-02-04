// <copyright file="BackgroundJobExecutor.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services.BackgroundJobs;

namespace SeguridadSocialApi.Services;

/// <summary>
/// Servicio de background que procesa jobs encolados de forma asíncrona.
/// </summary>
public class BackgroundJobExecutor : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<BackgroundJobExecutor> logger;
    private readonly IConfiguration configuration;
    private readonly JobQueue jobQueue;

    public BackgroundJobExecutor(
                                IServiceProvider serviceProvider,
                                ILogger<BackgroundJobExecutor> logger,
                                IConfiguration configuration)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.configuration = configuration;
        jobQueue = new JobQueue(logger);
    }

    /// <summary>
    /// Expone el ServiceProvider para que los controllers puedan acceder a servicios scoped.
    /// </summary>
    public IServiceProvider ServiceProvider => serviceProvider;

    /// <summary>
    /// Encola un job para su ejecución en background.
    /// </summary>
    public void EnqueueJob(string jobId)
    {
        jobQueue.Enqueue(jobId);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundJobExecutor iniciado");

        try
        {
            await ProcessJobsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown normal
            logger.LogInformation("BackgroundJobExecutor detenido");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fatal en BackgroundJobExecutor");
            throw;
        }
    }

    private async Task ProcessJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await jobQueue.DequeueAsync(stoppingToken);

                if (jobId != null)
                {
                    logger.LogInformation("Procesando job {JobId}", jobId);
                    _ = Task.Run(() => ExecuteJobInScopeAsync(jobId, stoppingToken), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en el loop principal de BackgroundJobExecutor");
            }
        }
    }

    private async Task ExecuteJobInScopeAsync(string jobId, CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();

        var jobManager = scope.ServiceProvider.GetRequiredService<IJobManager>();
        var jobAuditRepo = scope.ServiceProvider.GetRequiredService<IJobAuditRepository>();

        var jobAction = jobManager.GetJobAction(jobId);
        if (jobAction == null)
        {
            logger.LogWarning("Job {JobId} no tiene acción asociada", jobId);
            return;
        }

        jobManager.MarkJobAsStarted(jobId);

        var timeoutMinutes = configuration.GetValue<int>("BackgroundJobs:TimeoutMinutes", 60);
        var cancellationToken = jobManager.GetCancellationToken(jobId);

        var context = new JobExecutionContext
        {
            JobId = jobId,
            JobManager = jobManager,
            Logger = logger,
            JobAction = jobAction,
            CancellationToken = cancellationToken,
            TimeoutMinutes = timeoutMinutes,
        };

        var executor = new JobExecutor(jobAuditRepo, logger);
        await executor.ExecuteAsync(context);
    }
}
