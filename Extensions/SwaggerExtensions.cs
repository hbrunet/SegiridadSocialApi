// <copyright file="SwaggerExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Microsoft.OpenApi.Models;

namespace SeguridadSocialApi.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Seguridad Social API",
                Description = "API para gestión de declaraciones juradas de Seguridad Social",
                Contact = new OpenApiContact
                {
                    Name = "Equipo de Desarrollo",
                    Email = "desarrollo@example.com",
                },
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Autenticación JWT. Ingresa 'Bearer' seguido de un espacio y luego tu token. Ejemplo: 'Bearer abc123'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Seguridad Social API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Seguridad Social API - Documentación";
            options.DisplayRequestDuration();
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.EnableFilter();
        });

        return app;
    }
}
