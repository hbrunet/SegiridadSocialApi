// <copyright file="IConfiguracionRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories;

public interface IConfiguracionRepository
{
    Task<List<TipoHojaDto>> GetTiposHojaExternosAsync();

    Task<List<GrupoAdicionalDto>> GetGruposAdicionalesAsync();

    Task<List<TipoLiquidacionDto>> GetTiposLiquidacionAsync();

    Task<List<ReparticionDto>> GetReparticionesSegSocialAsync();

    Task<List<EstadoDto>> GetEstadosHojaAsync();
}
