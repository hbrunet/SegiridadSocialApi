// <copyright file="CrearHojaRequest.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Controllers.Requests;

public class CrearHojaRequest
{
    public long IdArchivo { get; set; }

    public int TipoNovedad { get; set; }

    public int GrupoAdicional { get; set; }

    public int TipoLiquidacion { get; set; }

    public int CantidadRegistros { get; set; }

    public DateTime Periodo { get; set; }

    public int IdRep { get; set; }

    // Opcional: identificar el flujo para reutilizar la misma sesión Oracle (tablas temporales)
    public string? FlowId { get; set; }
}
