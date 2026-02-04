// <copyright file="ConfiguracionController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionRepository configuracionRepository;

    public ConfiguracionController(IConfiguracionRepository configuracionRepository)
    {
        this.configuracionRepository = configuracionRepository;
    }

    [HttpGet("tipos-hoja")]
    public async Task<IActionResult> GetTiposHojaExternos()
    {
        var tiposHoja = await configuracionRepository.GetTiposHojaExternosAsync();
        return Ok(tiposHoja);
    }

    [HttpGet("grupos-adicionales")]
    public async Task<IActionResult> GetGruposAdicionales()
    {
        var gruposAdicionales = await configuracionRepository.GetGruposAdicionalesAsync();
        return Ok(gruposAdicionales);
    }

    [HttpGet("tipos-liquidacion")]
    public async Task<IActionResult> GetTiposLiquidacion()
    {
        var tiposLiquidacion = await configuracionRepository.GetTiposLiquidacionAsync();
        return Ok(tiposLiquidacion);
    }

    [HttpGet("reparticiones-seg-social")]
    public async Task<IActionResult> GetReparticionesSegSocial()
    {
        var reparticiones = await configuracionRepository.GetReparticionesSegSocialAsync();
        return Ok(reparticiones);
    }

    [HttpGet("estados-hoja")]
    public async Task<IActionResult> GetEstadosHoja()
    {
        var estadosHoja = await configuracionRepository.GetEstadosHojaAsync();
        return Ok(estadosHoja);
    }

    [HttpGet("job-types")]
    public async Task<IActionResult> GetJobTypes()
    {
        var jobTypes = await configuracionRepository.GetJobTypesAsync();
        return Ok(jobTypes);
    }
}
