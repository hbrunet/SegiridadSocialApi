// <copyright file="AuthData.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Datos de autenticación retornados por la API externa.
/// </summary>
public class AuthData
{
    /// <summary>
    /// Gets or sets la información del usuario.
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo User { get; set; } = new();

    /// <summary>
    /// Gets or sets el token JWT.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el tipo de token (Bearer).
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets el tiempo de expiración en segundos.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets la fecha/hora de expiración del token.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
