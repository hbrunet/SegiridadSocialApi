// <copyright file="HojasPaginadasDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.DTOs;

public class HojasPaginadasDto
{
    public int TotalRegistros { get; set; }

    public List<HojaDto> Hojas { get; set; } = new List<HojaDto>();
}
