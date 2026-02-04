// <copyright file="JobAuditLogger.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Services.BackgroundJobs;

/// <summary>
/// Logger que persiste mensajes tanto en archivos (Serilog) como en la base de datos (JOB_AUDIT_LOG).
/// </summary>
public sealed class JobAuditLogger
{
    private readonly string jobId;
    private readonly IJobAuditRepository jobAuditRepo;
    private readonly ILogger fallbackLogger;
    private int? currentProgress;

    public JobAuditLogger(
        string jobId,
        IJobAuditRepository jobAuditRepo,
        ILogger fallbackLogger)
    {
        this.jobId = jobId;
        this.jobAuditRepo = jobAuditRepo;
        this.fallbackLogger = fallbackLogger;
    }

    /// <summary>
    /// Registra un mensaje de información.
    /// </summary>
    public async Task LogInformationAsync(string message, int? progressPct = null)
    {
        await LogAsync("INFO", message, progressPct);
    }

    /// <summary>
    /// Registra un mensaje de advertencia.
    /// </summary>
    public async Task LogWarningAsync(string message, int? progressPct = null)
    {
        await LogAsync("WARNING", message, progressPct);
    }

    /// <summary>
    /// Registra un mensaje de error.
    /// </summary>
    public async Task LogErrorAsync(string message, Exception? ex = null, int? progressPct = null)
    {
        var fullMessage = ex != null ? $"{message} - {ex.Message}" : message;
        await LogAsync("ERROR", fullMessage, progressPct);
    }

    /// <summary>
    /// Registra un mensaje de debug.
    /// </summary>
    public async Task LogDebugAsync(string message, int? progressPct = null)
    {
        await LogAsync("DEBUG", message, progressPct);
    }

    /// <summary>
    /// Actualiza el progreso sin mensaje.
    /// </summary>
    public async Task UpdateProgressAsync(int progressPct)
    {
        currentProgress = progressPct;
        await LogAsync("INFO", $"Progreso: {progressPct}%", progressPct);
    }

    /// <summary>
    /// Método interno que realiza el logging dual (archivo + BD).
    /// </summary>
    private async Task LogAsync(string logLevel, string message, int? progressPct)
    {
        // Actualizar progreso actual si se proporciona
        if (progressPct.HasValue)
        {
            currentProgress = progressPct;
        }

        try
        {
            // 1. Loguear en archivo (Serilog)
            LogToFile(logLevel, message);

            // 2. Loguear en base de datos (JOB_AUDIT_LOG)
            await jobAuditRepo.AddJobLogAsync(
                                            jobId,
                                            logLevel,
                                            message,
                                            currentProgress);
        }
        catch (Exception ex)
        {
            // Si falla el logging en BD, al menos loguear en archivo
            fallbackLogger.LogWarning(ex,
                                      "Error al guardar log en BD para job {JobId}. Mensaje original: {Message}",
                                      jobId,
                                      message);
        }
    }

    /// <summary>
    /// Loguea en archivo usando Serilog.
    /// </summary>
    private void LogToFile(string logLevel, string message)
    {
        var enrichedMessage = $"[Job:{jobId}] {message}";

        switch (logLevel.ToUpperInvariant())
        {
            case "INFO":
                fallbackLogger.LogInformation(enrichedMessage);
                break;
            case "WARNING":
                fallbackLogger.LogWarning(enrichedMessage);
                break;
            case "ERROR":
                fallbackLogger.LogError(enrichedMessage);
                break;
            case "DEBUG":
                fallbackLogger.LogDebug(enrichedMessage);
                break;
            default:
                fallbackLogger.LogInformation(enrichedMessage);
                break;
        }
    }

    /// <summary>
    /// Versiones síncronas para compatibilidad con código existente.
    /// </summary>
    public void LogInformation(string message, int? progressPct = null)
    {
        _ = LogInformationAsync(message, progressPct);
    }

    public void LogWarning(string message, int? progressPct = null)
    {
        _ = LogWarningAsync(message, progressPct);
    }

    public void LogError(string message, Exception? ex = null, int? progressPct = null)
    {
        _ = LogErrorAsync(message, ex, progressPct);
    }

    public void LogDebug(string message, int? progressPct = null)
    {
        _ = LogDebugAsync(message, progressPct);
    }

    public void UpdateProgress(int progressPct)
    {
        _ = UpdateProgressAsync(progressPct);
    }
}
