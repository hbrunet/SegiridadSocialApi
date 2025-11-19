using SeguridadSocialApi.Common;

namespace SeguridadSocialApi.Services.Interfaces
{
    /// <summary>
    /// Servicio para normalización de archivos (encoding, caracteres especiales, etc.)
    /// </summary>
    public interface IFileNormalizationService
    {
        /// <summary>
        /// Normaliza un archivo a UTF-8 sin BOM aplicando todas las transformaciones configuradas
        /// </summary>
        Task<Result> NormalizeFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta el encoding de un archivo
        /// </summary>
        Result<(System.Text.Encoding Encoding, int BomLength)> DetectEncoding(byte[] bytes);

        /// <summary>
        /// Limpia caracteres problemáticos del contenido
        /// </summary>
        string CleanProblematicCharacters(string content);

        /// <summary>
        /// Reemplaza punto decimal por coma en números
        /// </summary>
        string ReplaceDecimalPoint(string content);

        /// <summary>
        /// Elimina líneas vacías al final del contenido
        /// </summary>
        string RemoveTrailingEmptyLines(string content);
    }
}
