// <copyright file="Startup.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using Newtonsoft.Json.Serialization;
using SeguridadSocialApi.Extensions;
using Serilog;

namespace SeguridadSocialApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                };
            });

        services.AddSwaggerConfiguration();
        services.AddCorsConfiguration();
        services.AddInfrastructure(Configuration);
        services.AddRepositories();
        services.AddApplicationServices();
        services.AddValidationRules();
        services.AddBackgroundJobs();

        // Nota: Usando middleware JWT personalizado debido a limitaciones de .NET 8
        // con validación de firma cuando la API externa no comparte la clave secreta.
        // Ver Middleware/JwtMiddleware.cs
        Log.Information("✅ Configuración completada. Autenticación JWT se maneja con middleware personalizado.");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwaggerConfiguration();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandling();
        app.UseRouting();
        app.UseCors("AllowAll");

        // Usar middleware JWT personalizado (sin validación de firma)
        app.UseMiddleware<Middleware.JwtMiddleware>();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
