// <copyright file="JobManager.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Services;

public interface IJobManager
{
    string CreateJob(Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description);

    void CreateJobWithId(string jobId, Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description);

    JobInfoDto? GetJobInfo(string jobId);

    void UpdateProgress(string jobId, int percentage, string? message = null);

    void CompleteJob(string jobId, object result);

    void FailJob(string jobId, string errorMessage);

    bool CancelJob(string jobId);

    void MarkJobAsStarted(string jobId);

    Func<IProgress<int>, CancellationToken, Task<object>>? GetJobAction(string jobId);

    CancellationToken GetCancellationToken(string jobId);

    void RegisterOracleConnection(string jobId, Oracle.ManagedDataAccess.Client.OracleConnection connection);

    void UnregisterOracleConnection(string jobId);
}

public class JobManager : IJobManager
{
    private readonly IMemoryCache cache;
    private readonly ConcurrentDictionary<string, JobInfoDto> jobs;
    private readonly ConcurrentDictionary<string, Func<IProgress<int>, CancellationToken, Task<object>>> jobActions;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> cancellationTokens;
    private readonly ConcurrentDictionary<string, Oracle.ManagedDataAccess.Client.OracleConnection> oracleConnections;
    private readonly ILogger<JobManager> logger;

    public JobManager(IMemoryCache cache, ILogger<JobManager> logger)
    {
        this.cache = cache;
        this.logger = logger;
        jobs = new ConcurrentDictionary<string, JobInfoDto>();
        jobActions = new ConcurrentDictionary<string, Func<IProgress<int>, CancellationToken, Task<object>>>();
        cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        oracleConnections = new ConcurrentDictionary<string, Oracle.ManagedDataAccess.Client.OracleConnection>();
    }

    /// <inheritdoc/>
    public string CreateJob(Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description)
    {
        var jobId = Guid.NewGuid().ToString("N");
        CreateJobWithId(jobId, jobAction, description);
        return jobId;
    }

    /// <inheritdoc/>
    public void CreateJobWithId(string jobId, Func<IProgress<int>, CancellationToken, Task<object>> jobAction, string description)
    {
        var cts = new CancellationTokenSource();

        var jobInfo = new JobInfoDto
        {
            JobId = jobId,
            Status = JobStatus.Pending,
            StatusMessage = description,
            ProgressPercentage = 0,
            CreatedAt = DateTime.UtcNow,
        };

        jobs[jobId] = jobInfo;
        jobActions[jobId] = jobAction;
        cancellationTokens[jobId] = cts;

        // Cache con expiración de 30 minutos después de completarse
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
        };
        cache.Set($"job:{jobId}", jobInfo, cacheOptions);

        logger.LogInformation("Job {JobId} creado: {Description}", jobId, description);
    }

    /// <inheritdoc/>
    public JobInfoDto? GetJobInfo(string jobId)
    {
        if (jobs.TryGetValue(jobId, out var jobInfo))
        {
            return jobInfo;
        }

        // Intentar recuperar del cache
        return cache.Get<JobInfoDto>($"job:{jobId}");
    }

    /// <inheritdoc/>
    public void UpdateProgress(string jobId, int percentage, string? message = null)
    {
        if (jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.ProgressPercentage = Math.Clamp(percentage, 0, 100);
            if (!string.IsNullOrEmpty(message))
            {
                jobInfo.StatusMessage = message;
            }

            cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
            });
        }
    }

    /// <inheritdoc/>
    public void CompleteJob(string jobId, object result)
    {
        if (jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Completed;
            jobInfo.CompletedAt = DateTime.UtcNow;
            jobInfo.Result = result;
            jobInfo.ProgressPercentage = 100;

            cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1), // Mantener resultado 1 hora
            });

            // Limpiar recursos
            jobActions.TryRemove(jobId, out _);
            cancellationTokens.TryRemove(jobId, out var cts);
            cts?.Dispose();

            // Limpiar conexión Oracle si existe
            oracleConnections.TryRemove(jobId, out var oracleConn);
            oracleConn?.Dispose();

            logger.LogInformation("Job {JobId} completado exitosamente", jobId);
        }
    }

    /// <inheritdoc/>
    public void FailJob(string jobId, string errorMessage)
    {
        if (jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Failed;
            jobInfo.CompletedAt = DateTime.UtcNow;
            jobInfo.ErrorMessage = errorMessage;

            cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            });

            // Limpiar recursos
            jobActions.TryRemove(jobId, out _);
            cancellationTokens.TryRemove(jobId, out var cts);
            cts?.Dispose();

            // Limpiar conexión Oracle si existe
            oracleConnections.TryRemove(jobId, out var oracleConn);
            oracleConn?.Dispose();

            logger.LogError("Job {JobId} falló: {Error}", jobId, errorMessage);
        }
    }

    /// <inheritdoc/>
    public bool CancelJob(string jobId)
    {
        if (cancellationTokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();

            if (jobs.TryGetValue(jobId, out var jobInfo))
            {
                jobInfo.Status = JobStatus.Cancelled;
                jobInfo.CompletedAt = DateTime.UtcNow;
                jobInfo.StatusMessage = "Job cancelado por el usuario";

                cache.Set($"job:{jobId}", jobInfo, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                });
            }

            // Remover conexión Oracle del tracking
            // Nota: La conexión se cerrará automáticamente cuando el SP termine
            // debido al 'using' o 'finally' block en el controller
            oracleConnections.TryRemove(jobId, out _);

            // Limpiar recursos
            jobActions.TryRemove(jobId, out _);
            cancellationTokens.TryRemove(jobId, out _);

            logger.LogInformation("Job {JobId} cancelado", jobId);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public Func<IProgress<int>, CancellationToken, Task<object>>? GetJobAction(string jobId)
    {
        jobActions.TryGetValue(jobId, out var action);
        return action;
    }

    /// <inheritdoc/>
    public CancellationToken GetCancellationToken(string jobId)
    {
        if (cancellationTokens.TryGetValue(jobId, out var cts))
        {
            return cts.Token;
        }

        return CancellationToken.None;
    }

    /// <inheritdoc/>
    public void MarkJobAsStarted(string jobId)
    {
        if (jobs.TryGetValue(jobId, out var jobInfo))
        {
            jobInfo.Status = JobStatus.Running;
            jobInfo.StartedAt = DateTime.UtcNow;
            cache.Set($"job:{jobId}", jobInfo);
            logger.LogInformation("Job {JobId} iniciado", jobId);
        }
    }

    /// <inheritdoc/>
    public void RegisterOracleConnection(string jobId, Oracle.ManagedDataAccess.Client.OracleConnection connection)
    {
        oracleConnections[jobId] = connection;
    }

    /// <inheritdoc/>
    public void UnregisterOracleConnection(string jobId)
    {
        oracleConnections.TryRemove(jobId, out _);
    }
}
