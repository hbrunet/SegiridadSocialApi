// <copyright file="Startup.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Net;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

        // Configurar Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Version = "v1",
                Title = "Seguridad Social API",
                Description = "API para gestión de declaraciones juradas de Seguridad Social",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Equipo de Desarrollo",
                    Email = "desarrollo@example.com"
                }
            });

            // Configurar autenticación JWT en Swagger
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "Autenticación JWT. Ingresa 'Bearer' seguido de un espacio y luego tu token. Ejemplo: 'Bearer abc123'",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Incluir comentarios XML (opcional - requiere habilitar generación de XML en .csproj)
            // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // if (File.Exists(xmlPath))
            // {
            //     options.IncludeXmlComments(xmlPath);
            // }
        });

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
        services.Configure<AuthOptions>(Configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(Configuration.GetSection(JwtOptions.SectionName));

        // Configurar HttpClient para AuthService con soporte de proxy
        services.AddHttpClient<IAuthService, AuthService>((serviceProvider, client) =>
        {
            var authOptions = Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
            client.BaseAddress = new Uri(authOptions?.BaseUrl ?? "http://svr-v-patri:85");
            client.Timeout = TimeSpan.FromSeconds(authOptions?.TimeoutSeconds ?? 30);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var proxyUrl = Configuration["Proxy:Url"];
            var proxyUser = Configuration["Proxy:Username"];
            var proxyPassword = Configuration["Proxy:Password"];
            var bypassProxy = Configuration.GetValue<bool>("Proxy:BypassForLocalAddresses", true);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };

            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler.Proxy = new WebProxy(proxyUrl)
                {
                    BypassProxyOnLocal = bypassProxy,
                    UseDefaultCredentials = false,
                };

                if (!string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPassword))
                {
                    handler.Proxy.Credentials = new NetworkCredential(proxyUser, proxyPassword);
                }
                else
                {
                    handler.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                }

                handler.UseProxy = true;
            }
            else
            {
                handler.UseProxy = false;
            }

            return handler;
        });

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

        // Habilitar Swagger y Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Seguridad Social API v1");
            options.RoutePrefix = "swagger"; // Acceder en /swagger
            options.DocumentTitle = "Seguridad Social API - Documentación";
            options.DisplayRequestDuration();

            // Configuración de UI mejorada
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.EnableFilter();
        });

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

        // Usar middleware JWT personalizado (sin validación de firma)
        app.UseMiddleware<SeguridadSocialApi.Middleware.JwtMiddleware>();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
