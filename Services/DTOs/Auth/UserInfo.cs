// <copyright file="UserInfo.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace SeguridadSocialApi.Services.DTOs.Auth;

/// <summary>
/// Información del usuario autenticado.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets or sets el ID del usuario.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets el nombre de usuario.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets la fecha de creación del usuario.
    /// </summary>
    [JsonPropertyName("creation_date")]
    public DateTime? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets el estado del usuario (OPEN, LOCKED, etc.).
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets los roles del usuario.
    /// </summary>
    [JsonPropertyName("roles")]
    public List<UserRole> Roles { get; set; } = new();

    /// <summary>
    /// Gets or sets si la cuenta está bloqueada.
    /// </summary>
    [JsonPropertyName("is_locked")]
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets el nombre para mostrar.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
