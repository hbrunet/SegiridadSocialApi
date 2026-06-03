// <copyright file="ExceptionHandlingExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Serilog;

namespace SeguridadSocialApi.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (ApplicationException ex)
            {
                Log.Warning(ex, "Error de aplicación en {Path}", context.Request.Path);
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                var result = JsonConvert.SerializeObject(new { error = ex.Message });
                await context.Response.WriteAsync(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error inesperado en {Path}", context.Request.Path);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var result = JsonConvert.SerializeObject(new { error = "Ocurrió un error inesperado.", detail = ex.Message });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
