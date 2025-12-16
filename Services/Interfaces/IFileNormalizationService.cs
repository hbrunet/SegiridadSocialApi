// <copyright file="IFileNormalizationService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Common;

namespace SeguridadSocialApi.Services.Interfaces;

/// <summary>
/// Servicio para normalización de archivos (encoding, caracteres especiales, etc.)
/// </summary>
public interface IFileNormalizationService
{
    /// <summary>
    /// Normaliza un archivo a UTF-8 sin BOM aplicando todas las transformaciones configuradas.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> NormalizeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detecta el encoding de un archivo.
    /// </summary>
    /// <returns></returns>
    Result<(System.Text.Encoding Encoding, int BomLength)> DetectEncoding(byte[] bytes);

    /// <summary>
    /// Limpia caracteres problemáticos del contenido.
    /// </summary>
    /// <returns></returns>
    string CleanProblematicCharacters(string content);

    /// <summary>
    /// Reemplaza punto decimal por coma en números.
    /// </summary>
    /// <returns></returns>
    string ReplaceDecimalPoint(string content);

    /// <summary>
    /// Elimina líneas vacías al final del contenido.
    /// </summary>
    /// <returns></returns>
    string RemoveTrailingEmptyLines(string content);
}
