// <copyright file="ValidacionArchivoDto.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.DTOs;

/// <summary>
/// Resultado de la validación de un archivo de novedades.
/// </summary>
public class ValidacionArchivoDto
{
    /// <summary>
    /// Gets or sets cantidad de registros válidos en el archivo.
    /// </summary>
    public int RegistrosValidos { get; set; }

    /// <summary>
    /// Gets or sets cantidad de registros con advertencias.
    /// </summary>
    public int RegistrosAdvertencias { get; set; }

    /// <summary>
    /// Gets or sets cantidad de registros con errores.
    /// </summary>
    public int RegistrosErrores { get; set; }

    /// <summary>
    /// Gets or sets lista detallada de errores encontrados.
    /// </summary>
    public List<DetalleValidacionDto> Errores { get; set; } = [];

    /// <summary>
    /// Gets or sets lista detallada de advertencias encontradas.
    /// </summary>
    public List<DetalleValidacionDto> Advertencias { get; set; } = [];
}

/// <summary>
/// Detalle de un error o advertencia de validación.
/// </summary>
public class DetalleValidacionDto
{
    /// <summary>
    /// Gets or sets mensaje descriptivo del error o advertencia.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets número de línea donde ocurre el error.
    /// </summary>
    public int Linea { get; set; }

    /// <summary>
    /// Gets or sets número de columna donde ocurre el error.
    /// </summary>
    public int Columna { get; set; }
}
