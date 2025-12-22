// <copyright file="UserRole.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Representa un rol de usuario en una aplicación específica.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets el ID del rol.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets el ID de la aplicación.
    /// </summary>
    [JsonPropertyName("application_id")]
    public int ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets el nombre del rol.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets el estado del rol (1 = activo).
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets el nombre de la aplicación.
    /// </summary>
    [JsonPropertyName("application_name")]
    public string ApplicationName { get; set; } = string.Empty;
}
