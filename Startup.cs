// <copyright file="Startup.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using FluentValidation;
using FluentValidation.AspNetCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;
using SeguridadSocialApi.Validaciones;
using SeguridadSocialApi.Validaciones.Rules;
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

        // Agregar FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<Startup>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });
        services.AddSingleton<IConfiguration>(provider =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build());

        services.AddMemoryCache();
        services.AddSingleton<IOracleConnectionFactory, OracleConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Configurar Options Pattern
        services.Configure<FileUploadOptions>(Configuration.GetSection(FileUploadOptions.SectionName));

        // Servicios de archivo
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IFileNormalizationService, FileNormalizationService>();

        // Repositorios y servicios refactorizados
        services.AddScoped<IHojaRepository, HojaRepository>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddScoped<IArchivoRepository, ArchivoRepository>();
        services.AddScoped<IJobProgressRepository, JobProgressRepository>();
        services.AddScoped<IJobAuditRepository, JobAuditRepository>();
        services.AddScoped<IDDJJRepository, DDJJRepository>();

        // services.AddScoped<IOracleService, OracleService>(); // Eliminado: ahora se usan los repositorios
        services.AddSingleton<IFlowSessionManager, FlowSessionManager>();
        services.AddScoped<FtpService>();
        services.AddScoped<NovedadesService>();

        // Registrar todas las reglas de validación
        services.AddScoped<IValidacionRule, ValidarFormatoCuilRule>();
        services.AddScoped<IValidacionRule, ValidarCamposObligatoriosGttRule>();
        services.AddScoped<IValidacionRule, ValidarCuilDuplicadoRule>();
        services.AddScoped<IValidacionRule, ValidarCodigoActividadRule>();
        services.AddScoped<IValidacionRule, ValidarTipoEmpresaRule>();
        services.AddScoped<IValidacionRule, ValidarCodigoCondicionRule>();
        services.AddScoped<IValidacionRule, ValidarRemuneracionPositivaRule>();
        services.AddScoped<IValidacionRule, ValidarObraSocialNacionalRule>();
        services.AddScoped<IValidacionRule, ValidarRangosCamposRule>();
        services.AddScoped<IValidacionRule, ValidarConsistenciaCamposRule>();

        // TODO: Agregar más reglas según necesidades específicas de negocio

        // Registrar el ejecutor de validaciones
        services.AddScoped<ValidacionExecutor>(provider =>
        {
            var reglas = provider.GetServices<IValidacionRule>();
            return new ValidacionExecutor(reglas);
        });

        // Background Jobs para SPs de larga duración
        services.AddSingleton<IJobManager, JobManager>();
        services.AddSingleton<BackgroundJobExecutor>();
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobExecutor>());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Agregar Serilog request logging
        app.UseSerilogRequestLogging();

        // Middleware global para manejo de excepciones (debe ir después de UseDeveloperExceptionPage)
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

        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
