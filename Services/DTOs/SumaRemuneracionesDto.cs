// <copyright file="SumaRemuneracionesDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System;

namespace SeguridadSocialApi.Services.DTOs;

public class SumaRemuneracionesDto
{
    // Usamos decimal para montos monetarios
    public decimal SumRem1 { get; set; }

    public decimal SumRem2 { get; set; }

    public decimal SumRem3 { get; set; }
}
