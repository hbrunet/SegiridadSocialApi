// <copyright file="AuthController.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes;
using SeguridadSocialApi.Services.DTOs.Auth;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Controllers;

/// <summary>
/// Controlador para operaciones de autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
                        IAuthService authService,
                        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de login que consume la API externa de autenticación.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <returns>Token JWT y datos del usuario si las credenciales son válidas.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.LoginAsync(
                                                        request.UserName,
                                                        request.Password);

            if (!response.Success || response.Data == null)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex, "Error de aplicación durante login");
            return StatusCode(503, new
            {
                success = false,
                message = "Servicio de autenticación no disponible",
                detail = ex.Message,
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante login");
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor",
                timestamp = DateTime.UtcNow,
            });
        }
    }

    /// <summary>
    /// Endpoint para obtener información del usuario actual desde el token.
    /// </summary>
    /// <returns>Información del usuario autenticado.</returns>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"]
           .ToString()
               .Replace("Bearer ", string.Empty);

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Token no proporcionado" });
            }

            var userInfo = _authService.GetUserInfoFromToken(token);

            if (userInfo == null)
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            return Ok(new
            {
                success = true,
                data = userInfo,
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario actual");
            return StatusCode(500, new
            {
                success = false,
                message = "Error al obtener información del usuario",
                timestamp = DateTime.UtcNow,
            });
        }
    }
}
