// <copyright file="ValidacionRuleBase.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Validaciones;

/// <summary>
/// Clase base abstracta para implementar reglas de validación
/// Proporciona funcionalidad común y estructura consistente.
/// </summary>
public abstract class ValidacionRuleBase : IValidacionRule
{
    /// <inheritdoc/>
    public abstract string NombreRegla { get; }

    /// <inheritdoc/>
    public abstract string Descripcion { get; }

    /// <inheritdoc/>
    public abstract TipoValidacion Tipo { get; }

    /// <inheritdoc/>
    public virtual int Orden => 100; // Orden por defecto

    /// <summary>
    /// Template method que ejecuta la validación y maneja errores.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<List<DetalleValidacionDto>> ValidarAsync(IDbConnection connection, long idArchivo)
    {
        try
        {
            return await EjecutarValidacionAsync(connection, idArchivo);
        }
        catch (Exception ex)
        {
            // Log del error (podrías inyectar ILogger aquí)
            return
        [
               new DetalleValidacionDto
      {
          Mensaje = $"Error al ejecutar regla '{NombreRegla}': {ex.Message}",
          Linea = 0,
          Columna = 0,
      }
            ];
        }
    }

    /// <summary>
    /// Método abstracto que cada regla debe implementar con su lógica específica.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected abstract Task<List<DetalleValidacionDto>> EjecutarValidacionAsync(IDbConnection connection, long idArchivo);

    /// <summary>
    /// Helper para crear detalles de validación de forma consistente.
    /// </summary>
    /// <returns></returns>
    protected DetalleValidacionDto CrearDetalle(string mensaje, int linea = 0, int columna = 0)
    {
        return new DetalleValidacionDto
        {
            Mensaje = mensaje,
            Linea = linea,
            Columna = columna,
        };
    }
}
