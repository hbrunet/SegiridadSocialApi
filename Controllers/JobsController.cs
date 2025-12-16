// <copyright file="JobsController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobManager jobManager;
    private readonly IJobAuditRepository jobAuditRepository;
    private readonly ILogger<JobsController> logger;

    public JobsController(
        IJobManager jobManager,
        IJobAuditRepository jobAuditRepository,
        ILogger<JobsController> logger)
    {
        this.jobManager = jobManager;
        this.jobAuditRepository = jobAuditRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Obtiene el estado actual de un job.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        var jobInfo = jobManager.GetJobInfo(jobId);
        if (jobInfo == null)
        {
            return NotFound(new { error = "Job no encontrado o expirado" });
        }

        logger.LogInformation(
            "Job {JobId}: {Progress}% - {Message}",
            jobInfo.JobId, jobInfo.ProgressPercentage, jobInfo.StatusMessage);
        return Ok(jobInfo);
    }

    /// <summary>
    /// Cancela un job en ejecución.
    /// </summary>
    /// <returns></returns>
    [HttpPost("{jobId}/cancel")]
    public IActionResult CancelJob(string jobId)
    {
        var success = jobManager.CancelJob(jobId);
        if (!success)
        {
            return NotFound(new { error = "Job no encontrado o ya finalizado" });
        }

        return Ok(new { message = "Job cancelado exitosamente" });
    }

    /// <summary>
    /// Obtiene el resultado de un job completado.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{jobId}/result")]
    public IActionResult GetJobResult(string jobId)
    {
        var jobInfo = jobManager.GetJobInfo(jobId);
        if (jobInfo == null)
        {
            return NotFound(new { error = "Job no encontrado o expirado" });
        }

        if (jobInfo.Status == JobStatus.Completed)
        {
            return Ok(jobInfo.Result);
        }

        if (jobInfo.Status == JobStatus.Failed)
        {
            return BadRequest(new { error = jobInfo.ErrorMessage });
        }

        return BadRequest(new { error = "Job aún no completado", status = jobInfo.Status });
    }

    /// <summary>
    /// Obtiene el historial de auditoría de un job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("{jobId}/audit")]
    public async Task<IActionResult> GetJobAudit(string jobId)
    {
        var audit = await jobAuditRepository.GetJobAuditAsync(jobId);
        if (audit == null)
        {
            return NotFound(new { error = "Auditoría no encontrada para este job" });
        }

        return Ok(audit);
    }

    /// <summary>
    /// Obtiene los logs detallados de un job.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("{jobId}/logs")]
    public async Task<IActionResult> GetJobLogs(string jobId)
    {
        var logs = await jobAuditRepository.GetJobLogsAsync(jobId);
        return Ok(logs);
    }
}
