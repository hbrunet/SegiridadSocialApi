// <copyright file="ValidationExtensions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using FluentValidation;
using FluentValidation.AspNetCore;
using SeguridadSocialApi.Validaciones;
using SeguridadSocialApi.Validaciones.Rules;

namespace SeguridadSocialApi.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidationRules(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<Startup>();

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

        services.AddScoped<ValidacionExecutor>(provider =>
        {
            var reglas = provider.GetServices<IValidacionRule>();
            return new ValidacionExecutor(reglas);
        });

        return services;
    }
}
