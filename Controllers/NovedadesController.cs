using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Validaciones;

namespace SeguridadSocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NovedadesController : ControllerBase
    {
        private readonly NovedadesService _novedadesService;
        private readonly ValidacionExecutor _validacionExecutor;

        public NovedadesController(NovedadesService novedadesService, ValidacionExecutor validacionExecutor)
        {
            _novedadesService = novedadesService;
            _validacionExecutor = validacionExecutor;
        }


        /// <summary>
        /// Carga un archivo y opcionalmente lo valida
        /// </summary>
        /// <param name="file">Archivo a cargar</param>
        /// <param name="tipoNovedad">Tipo de novedad</param>
        /// <param name="autoValidar">Si es true, ejecuta validaciones automáticamente y retorna resultados</param>
        [HttpPost]
        [Route("upload")]
        [RequestSizeLimit(100_000_000)] // Permite hasta 100 MB
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] int tipoNovedad,
            [FromForm] bool autoValidar = false)
        {
            if (file == null || file.Length == 0)
                throw new ApplicationException("No se recibió archivo.");

            if (tipoNovedad == 0)
                throw new ApplicationException("No se recibió el tipo de novedad.");

            var response = await _novedadesService.UploadFileAsync(file, tipoNovedad, autoValidar);
            return Ok(response);
        }

        [HttpPost]
        [Route("crear-hoja")]
        public async Task<IActionResult> CrearHoja([FromBody] CrearHojaRequest request)
        {
            if (request == null)
                throw new ApplicationException("El body de la solicitud no puede ser nulo.");

            if (request.CantidadRegistros <= 0)
                throw new ApplicationException("La cantidad de registros debe ser mayor a cero.");

            var response = await _novedadesService.CrearHojaAsync(request);
            return Ok(response);
        }

        [HttpPost]
        [Route("crear-hoja-async")]
        public IActionResult CrearHojaAsync([FromBody] CrearHojaRequest request)
        {
            if (request == null)
                throw new ApplicationException("El body de la solicitud no puede ser nulo.");

            if (request.CantidadRegistros <= 0)
                throw new ApplicationException("La cantidad de registros debe ser mayor a cero.");

            var response = _novedadesService.CrearHojaAsyncJob(request);
            return Accepted(response); // 202 Accepted para indicar procesamiento asíncrono
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
            var hojas = await _novedadesService.GetHojasAsync(periodo, estado, nroHoja, idRep, page, pageSize);
            return Ok(hojas);
        }

        [HttpPost("procesar-hoja/{id}")]
        public async Task<IActionResult> ProcesarHoja(int id)
        {
            if (id <= 0)
                throw new ApplicationException("El id de hoja debe ser mayor a cero.");

            await _novedadesService.ProcesarHojaAsync(id);

            return Ok(new
            {
                message = $"Hoja {id} procesada exitosamente",
                nroHoja = id,
                fechaProceso = DateTime.UtcNow
            });
        }

        [HttpPut("anular-hoja/{id}")]
        public async Task<IActionResult> AnularHoja(int id)
        {
            if (id <= 0)
                throw new ApplicationException("El id de hoja debe ser mayor a cero.");

            await _novedadesService.AnularHojaAsync(id);

            return Ok(new
            {
                message = $"Hoja {id} anulada exitosamente",
                nroHoja = id,
                fechaAnulacion = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Valida un archivo cargado. 
        /// Requiere flowId si se quiere validar inmediatamente después de upload (para mantener la sesión Oracle con la GTT)
        /// </summary>
        /// <param name="idArchivo">ID del archivo a validar</param>
        /// <param name="flowId">Opcional: ID del flujo para reutilizar la sesión Oracle</param>
        [HttpPost("validar-archivo/{idArchivo}")]
        public async Task<IActionResult> ValidarArchivo(int idArchivo, [FromQuery] string? flowId = null)
        {
            if (idArchivo <= 0)
                throw new ApplicationException("El id de archivo debe ser mayor a cero.");

            var response = await _novedadesService.ValidarArchivoAsync(idArchivo, flowId);

            return Ok(response);
        }

        /// <summary>
        /// Obtiene la lista de todas las reglas de validación registradas
        /// Útil para documentación y debugging
        /// </summary>
        [HttpGet("reglas-validacion")]
        public IActionResult ObtenerReglasValidacion()
        {
            var reglas = _validacionExecutor.ObtenerReglasRegistradas();
            return Ok(new
            {
                total = reglas.Count,
                reglas = reglas.OrderBy(r => r.Orden)
            });
        }
    }

}
