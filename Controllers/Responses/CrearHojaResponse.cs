// <copyright file="CrearHojaResponse.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Controllers.Responses;

public class CrearHojaResponse
{
    public long IdArchivo { get; set; }

    public int TipoNovedad { get; set; }

    public int GrupoAdicional { get; set; }

    public int TipoLiquidacion { get; set; }

    public int NroHoja { get; set; }
}
