using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    public DDJJController(
        ILogger<DDJJController> logger,
        BackgroundJobExecutor backgroundJobExecutor,
        IJobManager jobManager,
        IDDJJRepository ddjjRepository,
        IMemoryCache cache)
    {
        _logger = logger;
        _backgroundJobExecutor = backgroundJobExecutor;
        _jobManager = jobManager;
        _ddjjRepository = ddjjRepository;
        _cache = cache;
    }

    [HttpPost("fusionar-datos")]
    public async Task<IActionResult> FusionarDatos([FromBody] FusionDatosRequest request)
    {
        if (request == null || request.Periodo == default)
        {
            throw new ApplicationException("El periodo es obligatorio.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var endpoint = Request.Path.ToString();
        var username = HttpContext.User?.FindFirst("unique_name")?.Value;

        var helper = new BackgroundJobHelper(_backgroundJobExecutor.ServiceProvider, _logger);
        var auditHelper = new JobAuditHelper(
            HttpContext.RequestServices.GetRequiredService<IJobAuditRepository>(),
            _logger);

        _jobManager.CreateJobWithId(
            jobId,
            async (progress, cancellationToken) =>
            {
                // ✅ Usar el método con logging dual
                return await helper.ExecuteJobWithLoggingAsync(
                    jobId,
                    $"Fusionar Datos - Periodo {request.Periodo:yyyy-MM}, CUIL {request.Cuil}",
                    async (connection, jobLogger, ct) =>
                    {
                        try
                        {
                            // Ejecutar el SP
                            await _ddjjRepository.FusionarDatosAsync(connection, request.Periodo, request.Cuil, jobId);

                            return new FusionDatosResponse
                            {
                                Periodo = request.Periodo,
                                Estado = "COMPLETADO",
                                JobId = jobId,
                                Cuil = request.Cuil,
                            };
                        }
                        catch (Exception ex)
                        {
                            await jobLogger.LogErrorAsync("Error durante la fusión", ex);
                            throw;
                        }
                    });
            },
            $"Fusionar Datos - Periodo {request.Periodo:yyyy-MM}, CUIL {request.Cuil}");

        await auditHelper.InsertAuditAsync(
            jobId,
            request,
            endpoint,
            username);

        _backgroundJobExecutor.EnqueueJob(jobId);

        return Accepted(new StartJobResponse
        {
            JobId = jobId,
            Message = $"Fusión de datos iniciada para periodo {request.Periodo:yyyy-MM}.CUIL {request.Cuil}",
        });
    }


    /// <summary>
    /// Inicia la exportación de la presentación DDJJ de un periodo como archivo de texto.
    /// El proceso corre en background dado que puede generar archivos de gran tamaño.
    /// Usar GET /api/jobs/{jobId} para consultar el progreso.
    /// Una vez completado, descargar con GET /api/ddjj/exportar-presentacion/{jobId}/archivo.
    /// </summary>
    [HttpPost("exportar-presentacion")]
    public async Task<IActionResult> ExportarPresentacion([FromBody] ExportarPresentacionRequest request)
    {
        if (request == null || request.Periodo == default)
        {
            throw new ApplicationException("El periodo es obligatorio.");
        }

        var jobId = Guid.NewGuid().ToString("N");
        var endpoint = Request.Path.ToString();
        var username = HttpContext.User?.FindFirst("unique_name")?.Value;

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
                    $"Exportar Presentación - Periodo {request.Periodo:yyyy-MM}",
                    async (connection, jobLogger, ct) =>
                    {
                        try
                        {
                            // Contar registros como paso previo para reportar progreso real
                            _jobManager.UpdateProgress(jobId, 5, "Iniciando proceso de exportación...");
                            jobLogger.LogInformation("Iniciando proceso de exportación...", 5);

                            // Exportar a un MemoryStream; para periodos muy grandes
                            // se podría usar un archivo temporal en disco.
                            using var ms = new MemoryStream();
                            var cantRegistros = await _ddjjRepository.ExportarPresentacionAsync(
                                connection, request.Periodo, ms, ct);

                            _jobManager.UpdateProgress(jobId, 100, $"Exportación completa: {cantRegistros} registros");
                            jobLogger.LogInformation($"Exportación completa: {cantRegistros} registros", 100);

                            var nombreArchivo = $"PRESENTACION_{request.Periodo:yyyyMM}_{jobId}.txt";
                            var contenido = ms.ToArray();

                            // Almacenar el binario en cache separado del Result del job,
                            // evitando que se serialice a JSON en GET /api/jobs/{jobId}.
                            _cache.Set(
                                $"exportacion:{jobId}",
                                contenido,
                                TimeSpan.FromHours(1));

                            return new ExportarPresentacionJobResult
                            {
                                Periodo = request.Periodo,
                                CantRegistros = cantRegistros,
                                NombreArchivo = nombreArchivo,
                            };
                        }
                        catch (Exception ex)
                        {
                            await jobLogger.LogErrorAsync("Error durante la exportación", ex);
                            throw;
                        }
                    });
            },
            $"Exportar Presentación - Periodo {request.Periodo:yyyy-MM}");

        await auditHelper.InsertAuditAsync(jobId, request, endpoint, username);

        _backgroundJobExecutor.EnqueueJob(jobId);

        return Accepted(new
        {
            JobId = jobId,
            Message = $"Exportación iniciada para periodo {request.Periodo:yyyy-MM}. " +
                      $"Consulte el estado en GET /api/jobs/{jobId} y descargue el archivo con " +
                      $"GET /api/ddjj/exportar-presentacion/{jobId}/archivo una vez completado.",
        });
    }

    /// <summary>
    /// Descarga el archivo de presentación generado por el job indicado.
    /// El job debe estar en estado completado.
    /// </summary>
    [HttpGet("exportar-presentacion/{jobId}/archivo")]
    public IActionResult DescargarPresentacion(string jobId)
    {
        var jobInfo = _jobManager.GetJobInfo(jobId);
        if (jobInfo == null)
        {
            return NotFound(new { error = "Job no encontrado o expirado." });
        }

        if (jobInfo.Status != Services.DTOs.JobStatus.Completed)
        {
            return Conflict(new
            {
                error = "El job aún no finalizó.",
                estado = jobInfo.Status.ToString(),
                progreso = jobInfo.ProgressPercentage,
                mensaje = jobInfo.StatusMessage,
            });
        }

        if (jobInfo.Result is not ExportarPresentacionJobResult resultado)
        {
            return UnprocessableEntity(new { error = "El resultado del job no contiene un archivo válido." });
        }

        if (!_cache.TryGetValue($"exportacion:{jobId}", out byte[]? contenido) || contenido == null)
        {
            return StatusCode(410, new { error = "El archivo expiró del caché (TTL: 1 hora). Vuelva a iniciar la exportación." });
        }

        if (resultado.CantRegistros == 0)
        {
            return NoContent();
        }

        _logger.LogInformation(
            "Descarga de presentación {NombreArchivo} - {CantRegistros} registros, {Bytes} bytes",
            resultado.NombreArchivo, resultado.CantRegistros, contenido.Length);

        return File(contenido, "text/plain; charset=utf-8", resultado.NombreArchivo);
    }
}

/// <summary>
/// Resultado interno del job de exportación de presentación.
/// </summary>
file sealed class ExportarPresentacionJobResult
{
    public DateTime Periodo { get; set; }

    public int CantRegistros { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;
}
