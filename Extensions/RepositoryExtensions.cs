// <copyright file="RepositoryExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Repositories;

namespace SeguridadSocialApi.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IHojaRepository, HojaRepository>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddScoped<IArchivoRepository, ArchivoRepository>();
        services.AddScoped<IJobProgressRepository, JobProgressRepository>();
        services.AddScoped<IJobAuditRepository, JobAuditRepository>();
        services.AddScoped<IDDJJRepository, DDJJRepository>();

        return services;
    }
}
