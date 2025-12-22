// <copyright file="IAuthService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Services.DTOs.Auth;

namespace SeguridadSocialApi.Services.Interfaces;

/// <summary>
/// Servicio para interactuar con la API de autenticación externa.
/// </summary>
public interface IAuthService
{
    /// <summary>
  /// Autentica un usuario contra la API externa.
    /// </summary>
    /// <param name="userName">Nombre de usuario.</param>
    /// <param name="password">Contraseña del usuario.</param>
    /// <param name="applicationId">ID de la aplicación (default: 9 = DGS Intranet).</param>
    /// <returns>Response con el token JWT y datos del usuario.</returns>
    Task<AuthResponse> LoginAsync(string userName, string password, int applicationId = 9);

    /// <summary>
    /// Obtiene la información del usuario desde los claims del token JWT.
    /// </summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>Información básica del usuario extraída del token.</returns>
    UserInfo? GetUserInfoFromToken(string token);
}
