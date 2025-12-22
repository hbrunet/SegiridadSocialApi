using Dapper;
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.BackgroundJobs;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DDJJController : ControllerBase
{
    private readonly ILogger<DDJJController> _logger;
    private readonly BackgroundJobExecutor _backgroundJobExecutor;
    private readonly IJobManager _jobManager;
    private readonly IDDJJRepository _ddjjRepository;

    public DDJJController(
        ILogger<DDJJController> logger,
        BackgroundJobExecutor backgroundJobExecutor,
        IJobManager jobManager,
        IDDJJRepository ddjjRepository)
    {
        _logger = logger;
        _backgroundJobExecutor = backgroundJobExecutor;
        _jobManager = jobManager;
        _ddjjRepository = ddjjRepository;
    }

    [HttpPost("fusionar-datos")]
    public async Task<IActionResult> FusionarDatos([FromBody] FusionDatosRequest request)
    {
        if (request == null || request.Periodo == default)
        {
            throw new ApplicationException("El periodo es obligatorio.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var helper = new BackgroundJobHelper(_backgroundJobExecutor.ServiceProvider, _logger);
        var auditHelper = new JobAuditHelper(
            HttpContext.RequestServices.GetRequiredService<IJobAuditRepository>(),
            _logger);

        _jobManager.CreateJobWithId(
            jobId,
            async (progress, cancellationToken) =>
            {
                return await helper.ExecuteJobAsync(
                    jobId,
                    $"Fusionar Datos - Periodo {request.Periodo:yyyy-MM}",
                    async (connection, ct) =>
                    {
                        await _ddjjRepository.FusionarDatosAsync(connection, request.Periodo, jobId);

                        return new FusionDatosResponse
                        {
                            Periodo = request.Periodo,
                            Estado = "COMPLETADO",
                            JobId = jobId,
                        };
                    });
            },
            $"Fusionar Datos - Periodo {request.Periodo:yyyy-MM}");

        _logger.LogInformation("Job {JobId} creado para fusionar datos del periodo {Periodo}", jobId, request.Periodo);

        await auditHelper.InsertAuditAsync(
            jobId,
            request,
            "FUSION_DATOS",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString());

        _backgroundJobExecutor.EnqueueJob(jobId);

        return Accepted(new StartJobResponse
        {
            JobId = jobId,
            Message = $"Fusión de datos iniciada para periodo {request.Periodo:yyyy-MM}",
        });
    }


    [HttpPost("generar-presentacion")]
    public async Task<IActionResult> GenerarPresentacion(
        [FromQuery] DateTime periodo)
    {
       throw new NotImplementedException();
    }
}
