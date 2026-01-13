// <copyright file="ExternalLoginRequest.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Request interno para login en la API de autenticación externa (incluye ApplicationId).
/// </summary>
internal class ExternalLoginRequest
{
    /// <summary>
    /// Gets or sets el nombre de usuario.
    /// </summary>
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets la contraseña del usuario.
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el ID de la aplicación.
    /// </summary>
    [JsonPropertyName("application_id")]
    public int ApplicationId { get; set; }
}
