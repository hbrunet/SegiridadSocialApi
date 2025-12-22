// <copyright file="AuthorizeAttribute.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SeguridadSocialApi.Attributes;

/// <summary>
/// Atributo de autorización personalizado que verifica si el usuario está autenticado.
/// Reemplaza [Authorize] cuando no se usa el middleware JWT estándar de ASP.NET Core.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "No autorizado. Se requiere autenticación.",
                timestamp = DateTime.UtcNow
            })
            {
                StatusCode = 401
            };
        }
    }
}
