using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionRepository _configuracionRepository;

        public ConfiguracionController(IConfiguracionRepository configuracionRepository)
        {
            _configuracionRepository = configuracionRepository;
        }

        [HttpGet("tipos-hoja")]
        public async Task<IActionResult> GetTiposHojaExternos()
        {
            var tiposHoja = await _configuracionRepository.GetTiposHojaExternosAsync();
            return Ok(tiposHoja);
        }

        [HttpGet("grupos-adicionales")]
        public async Task<IActionResult> GetGruposAdicionales()
        {
            var gruposAdicionales = await _configuracionRepository.GetGruposAdicionalesAsync();
            return Ok(gruposAdicionales);
        }

        [HttpGet("tipos-liquidacion")]
        public async Task<IActionResult> GetTiposLiquidacion()
        {
            var tiposLiquidacion = await _configuracionRepository.GetTiposLiquidacionAsync();
            return Ok(tiposLiquidacion);
        }

        [HttpGet("reparticiones-seg-social")]
        public async Task<IActionResult> GetReparticionesSegSocial()
        {
            var reparticiones = await _configuracionRepository.GetReparticionesSegSocialAsync();
            return Ok(reparticiones);
        }

        [HttpGet("estados-hoja")]
        public async Task<IActionResult> GetEstadosHoja()
        {
            var estadosHoja = await _configuracionRepository.GetEstadosHojaAsync();
            return Ok(estadosHoja);
        }
    }
}