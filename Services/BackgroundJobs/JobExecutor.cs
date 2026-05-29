// <copyright file="JobExecutor.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Responsable de ejecutar un job individual y manejar su ciclo de vida.
/// </summary>
internal sealed class JobExecutor
{
    private readonly IJobAuditRepository jobAuditRepo;
    private readonly ILogger logger;

    public JobExecutor(IJobAuditRepository jobAuditRepo, ILogger logger)
    {
        this.jobAuditRepo = jobAuditRepo;
        this.logger = logger;
    }

    /// <summary>
    /// Ejecuta un job con toda la lógica de manejo de errores, timeouts y auditoría.
    /// </summary>
    public async Task ExecuteAsync(JobExecutionContext context)
    {
        try
        {
            await EnsureJobAuditExistsAsync(context);
            await MarkJobAsStartedAsync(context.JobId);
            await ExecuteJobWithTimeoutAsync(context);
        }
        catch (OperationCanceledException)
        {
            await HandleCancellationAsync(context);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(context.JobId, context.JobManager, ex);
        }
    }

    private async Task EnsureJobAuditExistsAsync(JobExecutionContext context)
    {
        try
        {
            // Verificar si ya existe el registro de auditoría
            var existingAudit = await jobAuditRepo.GetJobAuditAsync(context.JobId);

            if (existingAudit == null)
            {
                // Si no existe, crear el registro inicial
                var jobInfo = context.JobManager.GetJobInfo(context.JobId);

                logger.LogDebug(
                    "Creando registro de auditoría inicial para job {JobId}",
                    context.JobId);

                await jobAuditRepo.InsertJobAuditAsync(
                    context.JobId,
                    endpoint: "background-job", // Endpoint genérico para jobs creados sin auditoría inicial
                    inputParamsJson: jobInfo?.StatusMessage ?? "N/A",
                    createdBy: null);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al verificar/crear auditoría inicial para job {JobId}", context.JobId);
        }
    }

    private async Task MarkJobAsStartedAsync(string jobId)
    {
        try
        {
            await jobAuditRepo.MarkJobAsStartedAsync(jobId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al marcar job como iniciado en auditoría");
        }
    }

    private async Task ExecuteJobWithTimeoutAsync(JobExecutionContext context)
    {
        var progress = new Progress<int>(percentage =>
                 context.JobManager.UpdateProgress(context.JobId, percentage));

        using var executionToken = CreateExecutionToken(context);

        var result = await context.JobAction(progress, executionToken.Token);

        await CompleteJobSuccessfullyAsync(context.JobId, context.JobManager, result);
    }

    private TimeoutCancellationTokenSource CreateExecutionToken(JobExecutionContext context)
    {
        if (context.TimeoutMinutes > 0)
        {
            logger.LogDebug(
                                    "Job {JobId} ejecutándose con timeout de {Timeout} minutos",
                                    context.JobId,
                                    context.TimeoutMinutes);

            return new TimeoutCancellationTokenSource(
                                                        context.CancellationToken,
                                                        TimeSpan.FromMinutes(context.TimeoutMinutes));
        }

        logger.LogWarning("Job {JobId} ejecutándose SIN timeout automático", context.JobId);
        return new TimeoutCancellationTokenSource(context.CancellationToken, null);
    }

    private async Task CompleteJobSuccessfullyAsync(string jobId, IJobManager jobManager, object result)
    {
        jobManager.CompleteJob(jobId, result);

        try
        {
            var jobInfo = jobManager.GetJobInfo(jobId);
            var resultJson = result != null ? JsonSerializer.Serialize(result) : null;

            await jobAuditRepo.CompleteJobAuditAsync(
                                                        jobId,
                                                        "COMPLETADO",
                                                        jobInfo?.ProgressPercentage ?? 100,
                                                        resultJson);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al guardar resultado en auditoría");
        }
    }

    private async Task HandleCancellationAsync(JobExecutionContext context)
    {
        var jobInfo = context.JobManager.GetJobInfo(context.JobId);

        if (jobInfo?.Status == JobStatus.Cancelled)
        {
            logger.LogWarning("Job {JobId} fue cancelado por el usuario", context.JobId);
        }
        else
        {
            await HandleTimeoutAsync(context.JobId, context.JobManager);
        }
    }

    private async Task HandleTimeoutAsync(string jobId, IJobManager jobManager)
    {
        const string errorMsg = "Job cancelado o timeout excedido";
        jobManager.FailJob(jobId, errorMsg);

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

    private async Task HandleErrorAsync(string jobId, IJobManager jobManager, Exception ex)
    {
        logger.LogError(ex, "Error ejecutando job {JobId}", jobId);
        jobManager.FailJob(jobId, $"Error: {ex.Message}");

        try
        {
            await jobAuditRepo.CompleteJobAuditAsync(
                                                    jobId,
                                                    "ERROR",
                                                    0,
                                                    null,
                                                    ex.Message);
        }
        catch (Exception auditEx)
        {
            logger.LogWarning(auditEx, "Error al guardar error en auditoría");
        }
    }

    /// <summary>
    /// Maneja la creación de un token de cancelación con timeout opcional.
    /// </summary>
    private sealed class TimeoutCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource? timeoutCts;
        private readonly CancellationTokenSource? linkedCts;

        public TimeoutCancellationTokenSource(CancellationToken cancellationToken, TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                timeoutCts = new CancellationTokenSource(timeout.Value);
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                                                                            cancellationToken,
                                                                            timeoutCts.Token);
                Token = linkedCts.Token;
            }
            else
            {
                Token = cancellationToken;
            }
        }

        public CancellationToken Token { get; }

        public void Dispose()
        {
            timeoutCts?.Dispose();
            linkedCts?.Dispose();
        }
    }
}
