// <copyright file="FileNormalizationService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SeguridadSocialApi.Common;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;
using Serilog;

namespace SeguridadSocialApi.Services;

/// <summary>
/// Servicio para normalización de archivos.
/// </summary>
public partial class FileNormalizationService : IFileNormalizationService
{
    private readonly FileUploadOptions _options;
    private readonly IFileStorageService _fileStorage;

    // Regex compilado en .NET 7+ para mejor performance
    [GeneratedRegex(@"(\d+)\.(\d{1,4})", RegexOptions.Compiled)]
    private static partial Regex DecimalPointRegex();

    private static readonly Dictionary<char, char> CharacterReplacements = new()
    {
    { '¿', '?' }, { '¡', '!' },
    { 'á', 'a' }, { 'é', 'e' }, { 'í', 'i' }, { 'ó', 'o' }, { 'ú', 'u' },
    { 'Á', 'A' }, { 'É', 'E' }, { 'Í', 'I' }, { 'Ó', 'O' }, { 'Ú', 'U' },
    { 'ñ', 'n' }, { 'Ñ', 'N' },
    { 'ü', 'u' }, { 'Ü', 'U' },
    };

    public FileNormalizationService(IOptions<FileUploadOptions> options, IFileStorageService fileStorage)
    {
        _options = options.Value;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc/>
    public async Task<Result> NormalizeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Leer archivo
            var bytesResult = await _fileStorage.ReadFileBytesAsync(filePath, cancellationToken);
            if (!bytesResult.IsSuccess)
            {
                return Result.Failure(bytesResult.Error!);
            }

            var bytes = bytesResult.Value!;

            // Detectar encoding
            var encodingResult = DetectEncoding(bytes);
            if (!encodingResult.IsSuccess)
            {
                return Result.Failure(encodingResult.Error!);
            }

            var (encoding, bomLength) = encodingResult.Value;

            // Leer contenido con el encoding detectado
            var content = bomLength > 0
    ? encoding.GetString(bytes, bomLength, bytes.Length - bomLength)
         : encoding.GetString(bytes);

            // Aplicar transformaciones según configuración
            if (_options.EnableCharacterCleaning)
            {
                content = CleanProblematicCharacters(content);
            }

            if (_options.EnableDecimalPointReplacement)
            {
                content = ReplaceDecimalPoint(content);
            }

            if (_options.EnableEmptyLinesRemoval)
            {
                content = RemoveTrailingEmptyLines(content);
            }

            var lineCount = content.Split('\n').Length;
            Log.Information("Archivo normalizado: {Lines} líneas, {Length} caracteres", lineCount, content.Length);

            // Escribir como UTF-8 sin BOM
            var writeResult = await _fileStorage.WriteFileAsync(filePath, content, cancellationToken);
            if (!writeResult.IsSuccess)
            {
                return Result.Failure(writeResult.Error!);
            }

            Log.Information("Archivo convertido exitosamente a UTF-8 sin BOM");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al normalizar archivo {FilePath}", filePath);
            return Result.Failure($"Error al normalizar archivo: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public Result<(Encoding Encoding, int BomLength)> DetectEncoding(byte[] bytes)
    {
        try
        {
            if (bytes == null || bytes.Length == 0)
            {
                return Result<(Encoding, int)>.Failure("El contenido está vacío");
            }

            Encoding encoding;
            int bomLength = 0;

            // Verificar BOM UTF-8 (EF BB BF)
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
                bomLength = 3;
                Log.Information("Archivo detectado como UTF-8 con BOM");
            }

            // Verificar BOM UTF-16 LE (FF FE)
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                bomLength = 2;
                Log.Information("Archivo detectado como UTF-16 LE con BOM");
            }

            // Verificar BOM UTF-16 BE (FE FF)
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                bomLength = 2;
                Log.Information("Archivo detectado como UTF-16 BE con BOM");
            }

            // Sin BOM: detectar por heurística
            else
            {
                bool isValidUtf8 = false;
                try
                {
                    var utf8Decoder = Encoding.UTF8.GetDecoder();
                    utf8Decoder.Fallback = DecoderFallback.ExceptionFallback;
                    utf8Decoder.GetCharCount(bytes, 0, bytes.Length, true);
                    isValidUtf8 = true;
                    Log.Information("Archivo detectado como UTF-8 sin BOM");
                }
                catch (DecoderFallbackException)
                {
                    isValidUtf8 = false;
                }

                encoding = isValidUtf8
            ? Encoding.UTF8
                      : Encoding.GetEncoding(1252); // Windows-1252 (ANSI Latin 1)

                if (!isValidUtf8)
                {
                    Log.Information("Archivo detectado como Windows-1252 (ANSI Latin 1)");
                }
            }

            return Result<(Encoding, int)>.Success((encoding, bomLength));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al detectar encoding");
            return Result<(Encoding, int)>.Failure($"Error al detectar encoding: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public string CleanProblematicCharacters(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var sb = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            if (CharacterReplacements.TryGetValue(c, out var replacement))
            {
                sb.Append(replacement);
            }
            else
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        Log.Information("Caracteres especiales normalizados a ASCII");
        return result;
    }

    /// <inheritdoc/>
    public string ReplaceDecimalPoint(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var regex = DecimalPointRegex();
        var matches = regex.Matches(content).Count;
        var result = regex.Replace(content, "$1,$2");

        if (matches > 0)
        {
            Log.Information("Reemplazados {Count} puntos decimales por comas", matches);
        }

        return result;
    }

    /// <inheritdoc/>
    public string RemoveTrailingEmptyLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int lastNonEmptyLine = lines.Length - 1;
        while (lastNonEmptyLine >= 0 && string.IsNullOrWhiteSpace(lines[lastNonEmptyLine]))
        {
            lastNonEmptyLine--;
        }

        if (lastNonEmptyLine < 0)
        {
            Log.Warning("Archivo completamente vacío después de eliminar líneas en blanco");
            return string.Empty;
        }

        var cleanedLines = lines.Take(lastNonEmptyLine + 1).ToArray();
        var result = string.Join(Environment.NewLine, cleanedLines);

        var removedCount = lines.Length - cleanedLines.Length;
        if (removedCount > 0)
        {
            Log.Information("Eliminadas {Count} líneas vacías al final del archivo", removedCount);
        }

        return result;
    }
}
