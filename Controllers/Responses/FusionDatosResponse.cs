// <copyright file="TestingController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Controllers.Responses;

public class FusionDatosResponse
{
    public DateTime Periodo { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

}
