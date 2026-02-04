// <copyright file="JobExecutionContext.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Contexto de ejecución de un job que contiene toda la información necesaria para su procesamiento.
/// </summary>
internal sealed class JobExecutionContext
{
    public required string JobId { get; init; }

    public required IJobManager JobManager { get; init; }

    public required ILogger Logger { get; init; }

    public required Func<IProgress<int>, CancellationToken, Task<object>> JobAction { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public int TimeoutMinutes { get; init; }
}
