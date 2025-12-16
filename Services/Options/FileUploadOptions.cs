// <copyright file="FileUploadOptions.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

namespace SeguridadSocialApi.Services.Options;

/// <summary>
/// Configuración para el proceso de upload de archivos.
/// </summary>
public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Gets or sets directorio local donde se guardan los archivos subidos temporalmente.
    /// </summary>
    public string UploadDirectory { get; set; } = "uploads";

    /// <summary>
    /// Gets or sets tamaño máximo de archivo en bytes (default: 100MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100 MB

    /// <summary>
    /// Gets or sets extensiones de archivo permitidas.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = new[] { ".txt", ".csv", ".dat" };

    /// <summary>
    /// Gets or sets prefijo para nombres de archivo en el servidor.
    /// </summary>
    public int FileNamePrefixLength { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether habilitar normalización automática de encoding.
    /// </summary>
    public bool EnableEncodingNormalization { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether habilitar limpieza automática de caracteres especiales.
    /// </summary>
    public bool EnableCharacterCleaning { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether habilitar reemplazo de punto decimal por coma.
    /// </summary>
    public bool EnableDecimalPointReplacement { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether habilitar eliminación de líneas vacías al final.
    /// </summary>
    public bool EnableEmptyLinesRemoval { get; set; } = true;
}
