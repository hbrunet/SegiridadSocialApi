// <copyright file="AuthService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SeguridadSocialApi.Services.DTOs.Auth;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;

namespace SeguridadSocialApi.Services;

/// <summary>
/// Implementación del servicio de autenticación que consume la API externa de DGS.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthOptions _options;

    public AuthService(
                        HttpClient httpClient,
                        ILogger<AuthService> logger,
                        IOptions<AuthOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> LoginAsync(string userName, string password, int applicationId)
    {
        try
        {
            var request = new LoginRequest
            {
                UserName = userName,
                Password = password,
                ApplicationId = applicationId,
            };

            var response = await _httpClient.PostAsJsonAsync(_options.LoginEndpoint, request);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                _logger.LogError("Response de autenticación es null para usuario {UserName}", userName);
                throw new ApplicationException("Error al procesar la respuesta de autenticación");
            }

            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al intentar login para usuario {UserName}", userName);
            throw new ApplicationException(
                $"No se pudo conectar con el servicio de autenticación: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante login para usuario {UserName}", userName);
            throw;
        }
    }

    /// <inheritdoc/>
    public UserInfo? GetUserInfoFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            var displayName = jwtToken.Claims.FirstOrDefault(c => c.Type == "display_name")?.Value;
            var userName = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return new UserInfo
            {
                Id = int.Parse(userId),
                Name = userName ?? string.Empty,
                DisplayName = displayName ?? string.Empty,
                Status = "OPEN",
                IsLocked = false,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer información del token JWT");
            return null;
        }
    }
}
