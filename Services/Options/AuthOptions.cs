// <copyright file="AuthOptions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.Options;

/// <summary>
/// Opciones de configuración para el servicio de autenticación externo.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "AuthApi";

    /// <summary>
    /// Gets or sets la URL base de la API de autenticación.
    /// Ejemplo: http://svr-v-patri:85.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el endpoint de login.
    /// Ejemplo: /api/auth/login.
    /// </summary>
    public string LoginEndpoint { get; set; } = "/api/auth/login";

    /// <summary>
    /// Gets or sets el timeout en segundos para requests HTTP.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets el número de reintentos en caso de fallo.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets el ID de la aplicación.
    /// Default: 9 (DGS Intranet).
    /// </summary>
  public int ApplicationId { get; set; } = 9;
}
