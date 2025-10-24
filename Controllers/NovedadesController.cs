using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Controllers.Requests;

namespace SeguridadSocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NovedadesController : ControllerBase
    {
        private readonly NovedadesService _novedadesService;

        public NovedadesController(NovedadesService novedadesService)
        {
            _novedadesService = novedadesService;
        }


        [HttpPost]
        [Route("upload")]
        [RequestSizeLimit(100_000_000)] // Permite hasta 100 MB
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int tipoNovedad)
        {
            if (file == null || file.Length == 0)
                throw new ApplicationException("No se recibió archivo.");

            if (tipoNovedad == 0)
                throw new ApplicationException("No se recibió el tipo de novedad.");

            var response = await _novedadesService.UploadFileAsync(file, tipoNovedad);
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
    }

}
