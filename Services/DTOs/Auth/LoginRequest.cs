// <copyright file="LoginRequest.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Request para login en la API de autenticación externa.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets el nombre de usuario.
    /// </summary>
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets la contraseña del usuario.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el ID de la aplicación.
    /// Default: 9 (DGS Intranet).
    /// </summary>
    [JsonPropertyName("application_id")]
    public int ApplicationId { get; set; }
}
