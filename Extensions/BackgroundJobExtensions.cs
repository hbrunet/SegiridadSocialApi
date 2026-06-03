// <copyright file="BackgroundJobExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Extensions;

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddSingleton<IJobManager, JobManager>();
        services.AddSingleton<BackgroundJobExecutor>();
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobExecutor>());

        return services;
    }
}
