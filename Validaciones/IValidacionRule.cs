// <copyright file="IValidacionRule.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Validaciones;

/// <summary>
/// Interfaz base para todas las reglas de validación de archivos.
/// </summary>
public interface IValidacionRule
{
    /// <summary>
    /// Gets nombre descriptivo de la regla de validación.
    /// </summary>
    string NombreRegla { get; }

    /// <summary>
    /// Gets descripción detallada de qué valida esta regla.
    /// </summary>
    string Descripcion { get; }

    /// <summary>
    /// Gets tipo de validación: Error o Advertencia.
    /// </summary>
    TipoValidacion Tipo { get; }

    /// <summary>
    /// Gets orden de ejecución de la regla (menor = primero).
    /// </summary>
    int Orden { get; }

    /// <summary>
    /// Ejecuta la validación contra la tabla temporal de Oracle.
    /// </summary>
    /// <param name="connection">Conexión a Oracle.</param>
    /// <param name="idArchivo">ID del archivo a validar.</param>
    /// <returns>Lista de detalles de validación encontrados.</returns>
    Task<List<DetalleValidacionDto>> ValidarAsync(IDbConnection connection, long idArchivo);
}

/// <summary>
/// Tipo de validación.
/// </summary>
public enum TipoValidacion
{
    /// <summary>
    /// Error crítico que impide el procesamiento.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Advertencia que no impide el procesamiento.
    /// </summary>
    Advertencia = 2,
}
