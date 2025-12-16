// <copyright file="IFileStorageService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Common;

namespace SeguridadSocialApi.Services.Interfaces;

/// <summary>
/// Servicio para operaciones de almacenamiento de archivos.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Guarda un archivo subido en el almacenamiento local.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<string>> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lee el contenido de un archivo como bytes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<byte[]>> ReadFileBytesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escribe contenido a un archivo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<string>> WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo del almacenamiento.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un archivo existe.
    /// </summary>
    /// <returns></returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Obtiene el directorio de uploads configurado.
    /// </summary>
    /// <returns></returns>
    string GetUploadDirectory();
}
