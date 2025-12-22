// <copyright file="AuthResponse.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Response completo de la API de autenticación externa.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets si la operación fue exitosa.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets el mensaje de respuesta.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets los datos de autenticación.
    /// </summary>
    [JsonPropertyName("data")]
    public AuthData? Data { get; set; }

    /// <summary>
    /// Gets or sets los detalles del error cuando la operación falla.
    /// </summary>
    [JsonPropertyName("error")]
    public ErrorDetails? Error { get; set; }

    /// <summary>
    /// Gets or sets el timestamp de la respuesta.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Detalles del error retornado por la API externa.
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// Gets or sets el código de error (ej: "01017").
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el detalle técnico del error.
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}
