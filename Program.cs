using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SeguridadSocialApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
                    
                    // Configurar URLs desde variables de entorno o usar valores por defecto
                    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
                               ?? "https://localhost:5001;http://localhost:5000";
                    webBuilder.UseUrls(urls);
                });
    }
}
