// <copyright file="JwtMiddleware.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Middleware;

/// <summary>
/// Middleware personalizado para autenticación JWT sin validación de firma.
/// Apropiado para aplicaciones internas donde la API externa no comparte la clave secreta.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var token = context.Request.Headers["Authorization"]
                                        .FirstOrDefault()
                                      ?.Replace("Bearer ", string.Empty);

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Parsear el token sin validar la firma
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Validar claims básicos
                var issuer = jwtToken.Issuer;
                var audience = jwtToken.Audiences.FirstOrDefault();
                var expiration = jwtToken.ValidTo;

                // Validar issuer
                if (issuer != "DGS.Services.WebApi.Security")
                {
                    _logger.LogWarning("Token rechazado: Issuer inválido '{Issuer}'", issuer);
                    context.Response.StatusCode = 401;
                    return;
                }

                // Validar audience
                if (audience != "DGS.Services.Clients")
                {
                    _logger.LogWarning("Token rechazado: Audience inválido '{Audience}'", audience);
                    context.Response.StatusCode = 401;
                    return;
                }

                // Validar expiración
                if (expiration < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token rechazado: Token expirado");
                    context.Response.StatusCode = 401;
                    return;
                }

                // Crear ClaimsPrincipal con los claims del token
                var claims = jwtToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                // Asignar el principal al contexto
                context.User = principal;

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
                var userName = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;

                _logger.LogInformation(
                                        "✅ Token validado. Usuario: {UserName} (ID: {UserId})",
                                        userName,
                                        userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al validar token JWT");
                // No establecer el usuario, pero continuar con el request
            }
        }

        await _next(context);
    }
}
