// <copyright file="JobAuditHelper.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Helper para insertar auditoría de jobs.
/// </summary>
public class JobAuditHelper
{
    private readonly IJobAuditRepository jobAuditRepo;
    private readonly ILogger logger;

    public JobAuditHelper(IJobAuditRepository jobAuditRepo, ILogger logger)
    {
        this.jobAuditRepo = jobAuditRepo;
        this.logger = logger;
    }

    /// <summary>
    /// Inserta auditoría para un job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InsertAuditAsync<TRequest>(
string jobId,
TRequest request,
string jobType,
string? ipAddress,
string? userAgent)
    {
        var inputJson = System.Text.Json.JsonSerializer.Serialize(request);

        try
        {
            await jobAuditRepo.InsertJobAuditAsync(
                jobId,
                $"{jobType} - Periodo {GetPeriodoFromRequest(request)}",
                jobType,
                inputJson,
                null,
                ipAddress,
                userAgent);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al insertar auditoría para job {JobId}", jobId);
        }
    }

    private string GetPeriodoFromRequest<TRequest>(TRequest request)
    {
        // Intentar obtener la propiedad Periodo usando reflection
        var periodoProperty = typeof(TRequest).GetProperty("Periodo");
        if (periodoProperty != null)
        {
            var periodo = periodoProperty.GetValue(request);
            if (periodo is DateTime dt)
            {
                return dt.ToString("yyyy-MM");
            }
        }

        return typeof(TRequest).Name;
    }
}
