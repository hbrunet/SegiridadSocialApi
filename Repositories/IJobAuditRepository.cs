// <copyright file="IJobAuditRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace SeguridadSocialApi.Repositories;

public interface IJobAuditRepository
{
    /// <summary>
    /// Inserta un nuevo registro de auditoría cuando se crea un job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<long> InsertJobAuditAsync(
                            string jobId,
                            string jobName,
                            string jobType,
                            string inputParamsJson,
                            string? usuario = null,
                            string? ipAddress = null,
                            string? userAgent = null);

    /// <summary>
    /// Actualiza el audit cuando el job inicia su ejecución.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task MarkJobAsStartedAsync(string jobId);

    /// <summary>
    /// Actualiza el audit cuando el job completa (éxito o error).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CompleteJobAuditAsync(
                            string jobId,
                            string status,
                            int progressPct,
                            string? resultDataJson = null,
                            string? errorMessage = null,
                            int? registrosProcesados = null,
                            int? registrosErrores = null);

    /// <summary>
    /// Agrega un log detallado durante la ejecución del job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddJobLogAsync(
                    string jobId,
                    string logLevel,
                    string logMessage,
                    int? progressPct = null);

    /// <summary>
    /// Obtiene el historial de un job específico.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<JobAuditDto?> GetJobAuditAsync(string jobId);

    /// <summary>
    /// Obtiene los logs detallados de un job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<List<JobAuditLogDto>> GetJobLogsAsync(string jobId);
}

public class JobAuditDto
{
    public long AuditId { get; set; }

    public string JobId { get; set; } = string.Empty;

    public string? InternalJobId { get; set; }

    public string JobName { get; set; } = string.Empty;

    public string? JobType { get; set; }

    public string? InputParams { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ProgressPct { get; set; }

    public string? ResultData { get; set; }

    public string? ErrorMessage { get; set; }

    public decimal? DurationSeconds { get; set; }

    public int? RegistrosProcesados { get; set; }

    public int? RegistrosErrores { get; set; }

    public string? Usuario { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public class JobAuditLogDto
{
    public long LogId { get; set; }

    public DateTime LogTimestamp { get; set; }

    public string LogLevel { get; set; } = string.Empty;

    public string? LogMessage { get; set; }

    public int? ProgressPct { get; set; }
}
