// <copyright file="ApplicationServiceExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IFileNormalizationService, FileNormalizationService>();
        services.AddSingleton<IFlowSessionManager, FlowSessionManager>();
        services.AddScoped<FtpService>();
        services.AddScoped<NovedadesService>();

        return services;
    }
}
