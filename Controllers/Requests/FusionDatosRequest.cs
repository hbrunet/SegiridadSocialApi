// <copyright file="FusionDatosRequest.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Controllers.Requests;

public class FusionDatosRequest
{
    /// <summary>
    /// Gets or sets periodo a procesar (formato: YYYYMM o DateTime).
    /// </summary>
    public DateTime Periodo { get; set; }

    /// <summary>
    /// Gets or sets CUIL del empleador/trabajador a procesar.
    /// </summary>
    public long? Cuil { get; set; }

    /// <summary>
    /// Gets or sets flowId opcional si viene de un upload previo.
    /// </summary>
    public string? FlowId { get; set; }
}
