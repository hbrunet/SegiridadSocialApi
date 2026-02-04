// <copyright file="TestingController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Dapper;
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.BackgroundJobs;
using SeguridadSocialApi.Services.DTOs;
using System.Security.Claims;

namespace SeguridadSocialApi.Controllers;

/// <summary>
/// Controlador para endpoints de testing y pruebas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestingController : ControllerBase
{
    private readonly IJobManager _jobManager;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly ILogger<TestingController> _logger;

    public TestingController(
        IJobManager jobManager,
        BackgroundJobExecutor backgroundJobExecutor,
        ILogger<TestingController> logger)
    {
        _jobManager = jobManager;
        _backgroundJobExecutor = backgroundJobExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Test de fusión rápido (30 segundos) - Asíncrono con progreso.
    /// </summary>
    /// <param name="request">Datos de la solicitud con el periodo a procesar.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost("fusion-quick-async")]
    public async Task<IActionResult> TestFusionQuickAsync([FromBody] FusionDatosRequest request)
    {
        if (request == null || request.Periodo == default)
        {
            throw new ApplicationException("El periodo es obligatorio.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var endpoint = Request.Path.ToString();
        var helper = new BackgroundJobHelper(_backgroundJobExecutor.ServiceProvider, _logger);
        var auditHelper = new JobAuditHelper(
                                            HttpContext.RequestServices.GetRequiredService<IJobAuditRepository>(),
                                            _logger);

        _jobManager.CreateJobWithId(
                    jobId,
                    async (progress, cancellationToken) =>
                    {
                        return await helper.ExecuteJobWithLoggingAsync(
                        jobId,
                        $"Test Fusion Quick - Periodo {request.Periodo:yyyy-MM}",
                        async (connection, jobLogger, ct) =>
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("p_periodo", request.Periodo, System.Data.DbType.DateTime, System.Data.ParameterDirection.Input);
                            parameters.Add("p_job_id", jobId, System.Data.DbType.String, System.Data.ParameterDirection.Input);

                            await connection.ExecuteAsync(
                                                        "SEGSOCIAL.JOBS_MONITOR.SP_TEST_FUSION_QUICK",
                                                        parameters,
                                                        commandType: System.Data.CommandType.StoredProcedure);

                            return new FusionDatosResponse
                            {
                                Periodo = request.Periodo,
                                Estado = "COMPLETADO",
                                Mensaje = $"Test rápido completado para periodo {request.Periodo:yyyy-MM}",
                                JobId = jobId,
                            };
                        });
                    },
                    $"Test Fusion Quick - Periodo {request.Periodo:yyyy-MM}");

        _logger.LogInformation("Job {JobId} creado", jobId);

        var username = HttpContext.User?.FindFirst("unique_name")?.Value;

        await auditHelper.InsertAuditAsync(
                                            jobId,
                                            request,
                                            endpoint,
                                            username);

        _backgroundJobExecutor.EnqueueJob(jobId);

        return Accepted(new StartJobResponse
        {
            JobId = jobId,
            Message = $"Test de fusión rápido iniciado para periodo {request.Periodo:yyyy-MM}",
        });
    }

    /// <summary>
    /// Test de fusión lento (2 minutos) - Asíncrono con progreso.
    /// </summary>
    /// <param name="request">Datos de la solicitud con el periodo a procesar.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost("fusion-slow-async")]
    public async Task<IActionResult> TestFusionSlowAsync([FromBody] FusionDatosRequest request)
    {
        if (request == null || request.Periodo == default)
        {
            throw new ApplicationException("El periodo es obligatorio.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var endpoint = Request.Path.ToString();
        var helper = new BackgroundJobHelper(_backgroundJobExecutor.ServiceProvider, _logger);
        var auditHelper = new JobAuditHelper(
            HttpContext.RequestServices.GetRequiredService<IJobAuditRepository>(),
            _logger);

        _jobManager.CreateJobWithId(
            jobId,
            async (progress, cancellationToken) =>
            {
                return await helper.ExecuteJobWithLoggingAsync (
                jobId,
                $"Test Fusion Slow - Periodo {request.Periodo:yyyy-MM}",
                async (connection, jobLogger, ct) =>
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("p_periodo", request.Periodo, System.Data.DbType.DateTime, System.Data.ParameterDirection.Input);
                    parameters.Add("p_job_id", jobId, System.Data.DbType.String, System.Data.ParameterDirection.Input);

                    await connection.ExecuteAsync(
                    "SEGSOCIAL.JOBS_MONITOR.SP_TEST_FUSION_SLOW",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                    return new FusionDatosResponse
                    {
                    Periodo = request.Periodo,
                    Estado = "COMPLETADO",
                    Mensaje = $"Test lento completado para periodo {request.Periodo:yyyy-MM}",
                    JobId = jobId,
                    };
                });
            },
            $"Test Fusion Slow - Periodo {request.Periodo:yyyy-MM}");

        _logger.LogInformation("Job {JobId} creado", jobId);

        var username = HttpContext.User?.FindFirst("unique_name")?.Value;

        await auditHelper.InsertAuditAsync(
                                            jobId,
                                            request,
                                            endpoint,
                                            username);

        _backgroundJobExecutor.EnqueueJob(jobId);

        return Accepted(new StartJobResponse
        {
            JobId = jobId,
            Message = $"Test de fusión lento iniciado para periodo {request.Periodo:yyyy-MM}",
        });
    }
}
