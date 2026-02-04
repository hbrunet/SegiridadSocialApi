// <copyright file="JobQueue.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Collections.Concurrent;

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Cola thread-safe para jobs pendientes de ejecución.
/// </summary>
internal sealed class JobQueue
{
    private readonly ConcurrentQueue<string> queue;
    private readonly SemaphoreSlim signal;
    private readonly ILogger logger;

    public JobQueue(ILogger logger)
    {
        queue = new ConcurrentQueue<string>();
        signal = new SemaphoreSlim(0);
        this.logger = logger;
    }

    /// <summary>
    /// Encola un job para ejecución.
    /// </summary>
    public void Enqueue(string jobId)
    {
        queue.Enqueue(jobId);
        signal.Release();
        logger.LogInformation("Job {JobId} encolado para ejecución", jobId);
    }

    /// <summary>
    /// Espera hasta que haya un job disponible y lo retorna.
    /// </summary>
    public async Task<string?> DequeueAsync(CancellationToken cancellationToken)
    {
        await signal.WaitAsync(cancellationToken);

        if (queue.TryDequeue(out var jobId))
        {
            return jobId;
        }

        return null;
    }
}
