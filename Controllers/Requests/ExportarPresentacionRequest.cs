// <copyright file="ExportarPresentacionRequest.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Controllers.Requests;

public class ExportarPresentacionRequest
{
    /// <summary>
    /// Gets or sets periodo a exportar (formato: YYYYMM o DateTime).
    /// </summary>
    public DateTime Periodo { get; set; }
}
