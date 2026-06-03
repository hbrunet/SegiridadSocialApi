// <copyright file="InfrastructureExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Net;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;

namespace SeguridadSocialApi.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConfiguration>(_ =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build());

        services.AddMemoryCache();
        services.AddSingleton<IOracleConnectionFactory, OracleConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddHttpClient<IAuthService, AuthService>((_, client) =>
        {
            var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
            client.BaseAddress = new Uri(authOptions?.BaseUrl ?? "http://svr-v-patri:85");
            client.Timeout = TimeSpan.FromSeconds(authOptions?.TimeoutSeconds ?? 30);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var proxyUrl = configuration["Proxy:Url"];
            var proxyUser = configuration["Proxy:Username"];
            var proxyPassword = configuration["Proxy:Password"];
            var bypassProxy = configuration.GetValue<bool>("Proxy:BypassForLocalAddresses", true);

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

        return services;
    }
}
