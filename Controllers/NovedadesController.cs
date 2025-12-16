// <copyright file="NovedadesController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Dapper;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.BackgroundJobs;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Validaciones;

namespace SeguridadSocialApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NovedadesController : ControllerBase
{
    private readonly NovedadesService novedadesService;
    private readonly ValidacionExecutor validacionExecutor;
    private readonly IJobManager jobManager;
    private readonly BackgroundJobExecutor backgroundJobExecutor;
    private readonly ILogger<NovedadesController> logger;

    public NovedadesController(
        NovedadesService novedadesService,
        ValidacionExecutor validacionExecutor,
        IJobManager jobManager,
        BackgroundJobExecutor backgroundJobExecutor,
        ILogger<NovedadesController> logger)
    {
        this.novedadesService = novedadesService;
        this.validacionExecutor = validacionExecutor;
        this.jobManager = jobManager;
        this.backgroundJobExecutor = backgroundJobExecutor;
        this.logger = logger;
    }

    /// <summary>
    /// Carga un archivo y opcionalmente lo valida.
    /// </summary>
    /// <param name="file">Archivo a cargar.</param>
    /// <param name="tipoNovedad">Tipo de novedad.</param>
    /// <param name="autoValidar">Si es true, ejecuta validaciones automáticamente y retorna resultados.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost]
    [Route("upload")]
    [RequestSizeLimit(100_000_000)] // Permite hasta 100 MB
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] int tipoNovedad,
        [FromForm] bool autoValidar = false)
    {
        if (file == null || file.Length == 0)
        {
            throw new ApplicationException("No se recibió archivo.");
        }

        if (tipoNovedad == 0)
        {
            throw new ApplicationException("No se recibió el tipo de novedad.");
        }

        var response = await novedadesService.UploadFileAsync(file, tipoNovedad, autoValidar);
        return Ok(response);
    }

    [HttpPost]
    [Route("crear-hoja")]
    public async Task<IActionResult> CrearHoja([FromBody] CrearHojaRequest request)
    {
        if (request == null)
        {
            throw new ApplicationException("El body de la solicitud no puede ser nulo.");
        }

        if (request.CantidadRegistros <= 0)
        {
            throw new ApplicationException("La cantidad de registros debe ser mayor a cero.");
        }

        var response = await novedadesService.CrearHojaAsync(request);
        return Ok(response);
    }

    [HttpGet("listado-hojas")]
    public async Task<IActionResult> GetHojas(
        [FromQuery] DateTime? periodo,
        [FromQuery] int? estado,
        [FromQuery] int? nroHoja,
        [FromQuery] int? idRep,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var hojas = await novedadesService.GetHojasAsync(periodo, estado, nroHoja, idRep, page, pageSize);
        return Ok(hojas);
    }

    [HttpPost("procesar-hoja/{id}")]
    public async Task<IActionResult> ProcesarHoja(int id)
    {
        if (id <= 0)
        {
            throw new ApplicationException("El id de hoja debe ser mayor a cero.");
        }

        await novedadesService.ProcesarHojaAsync(id);

        return Ok(new
        {
            message = $"Hoja {id} procesada exitosamente",
            nroHoja = id,
            fechaProceso = DateTime.UtcNow,
        });
    }

    [HttpPut("anular-hoja/{id}")]
    public async Task<IActionResult> AnularHoja(int id)
    {
        if (id <= 0)
        {
            throw new ApplicationException("El id de hoja debe ser mayor a cero.");
        }

        await novedadesService.AnularHojaAsync(id);

        return Ok(new
        {
            message = $"Hoja {id} anulada exitosamente",
            nroHoja = id,
            fechaAnulacion = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Valida un archivo cargado.
    /// Requiere flowId si se quiere validar inmediatamente después de upload (para mantener la sesión Oracle con la GTT).
    /// </summary>
    /// <param name="idArchivo">ID del archivo a validar.</param>
    /// <param name="flowId">Opcional: ID del flujo para reutilizar la sesión Oracle.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost("validar-archivo/{idArchivo}")]
    public async Task<IActionResult> ValidarArchivo(int idArchivo, [FromQuery] string? flowId = null)
    {
        if (idArchivo <= 0)
        {
            throw new ApplicationException("El id de archivo debe ser mayor a cero.");
        }

        var response = await novedadesService.ValidarArchivoAsync(idArchivo, flowId);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene la lista de todas las reglas de validación registradas
    /// Útil para documentación y debugging.
    /// </summary>
    /// <returns></returns>
    [HttpGet("reglas-validacion")]
    public IActionResult ObtenerReglasValidacion()
    {
        var reglas = validacionExecutor.ObtenerReglasRegistradas();
        return Ok(new
        {
            total = reglas.Count,
            reglas = reglas.OrderBy(r => r.Orden),
        });
    }
}
