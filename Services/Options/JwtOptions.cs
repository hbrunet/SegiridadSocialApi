// <copyright file="JwtOptions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.Options;

/// <summary>
/// Opciones de configuración para validación de JWT.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets el issuer esperado del token JWT.
    /// </summary>
    public string Issuer { get; set; } = "DGS.Services.WebApi.Security";

    /// <summary>
    /// Gets or sets el audience esperado del token JWT.
    /// </summary>
    public string Audience { get; set; } = "DGS.Services.Clients";

    /// <summary>
    /// Gets or sets la clave secreta para validar el JWT.
    /// Debe ser la misma que usa la API de autenticación.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets si se debe validar el lifetime del token.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Gets or sets el clock skew en minutos (tolerancia de tiempo).
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}
