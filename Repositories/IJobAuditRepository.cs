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
                                    string endpoint,
                                    string inputParamsJson,
                                    string? createdBy = null);

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
                                string? errorMessage = null);

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

    /// <summary>
    /// Obtiene auditorías de jobs con filtros opcionales y paginación.
    /// </summary>
    /// <param name="createdBy">Filtro opcional por usuario que creó el job.</param>
    /// <param name="fechaInicio">Filtro opcional por fecha de creación.</param>
    /// <param name="endpoint">Filtro opcional por endpoint.</param>
    /// <param name="jobId">Filtro opcional por ID del job.</param>
    /// <param name="page">Número de página (default: 1).</param>
    /// <param name="pageSize">Tamaño de página (default: 10).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<JobAuditsPaginadasDto> GetJobAuditsAsync(
                                                string? createdBy = null,
                                                DateTime? fechaInicio = null,
                                                int? jobType = null,
                                                string? jobId = null,
                                                DateTime? periodo = null,
                                                int page = 1,
                                                int pageSize = 10);
}

public class JobAuditDto
{
    public long AuditId { get; set; }

    public string JobId { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

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

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public class JobAuditLogDto
{
    public long LogId { get; set; }

    public DateTime LogTimestamp { get; set; }

    public string LogLevel { get; set; } = string.Empty;

    public string? LogMessage { get; set; }

    public int? ProgressPct { get; set; }
}

public class JobAuditsPaginadasDto
{
    public int TotalRegistros { get; set; }

    public List<JobAuditDto> Audits { get; set; } = new();
}
