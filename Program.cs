// <copyright file="Program.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SeguridadSocialApi;

public class Program
{
    public static void Main(string[] args)
    {
        // Registrar proveedores de encoding para soportar Windows-1252 (ANSI)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Iniciando aplicación SeguridadSocialApi");
            Log.Information("Proveedor de encodings registrado - Soporta Windows-1252, ISO-8859-1, etc.");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación falló al iniciar");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog() // Usar Serilog para logging
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                // Kestrel leerá automáticamente la configuración desde appsettings.json
                // La sección "Kestrel:Endpoints" define las URLs de HTTP y HTTPS
            });
}
