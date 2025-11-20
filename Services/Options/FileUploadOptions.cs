namespace SeguridadSocialApi.Services.Options
{
    /// <summary>
    /// Configuración para el proceso de upload de archivos
    /// </summary>
    public class FileUploadOptions
    {
        public const string SectionName = "FileUpload";

        /// <summary>
        /// Directorio local donde se guardan los archivos subidos temporalmente
        /// </summary>
        public string UploadDirectory { get; set; } = "uploads";

        /// <summary>
        /// Tamaño máximo de archivo en bytes (default: 100MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100 MB

        /// <summary>
        /// Extensiones de archivo permitidas
        /// </summary>
        public string[] AllowedExtensions { get; set; } = new[] { ".txt", ".csv", ".dat" };

        /// <summary>
        /// Prefijo para nombres de archivo en el servidor
        /// </summary>
        public int FileNamePrefixLength { get; set; } = 3;

        /// <summary>
        /// Habilitar normalización automática de encoding
        /// </summary>
        public bool EnableEncodingNormalization { get; set; } = true;

        /// <summary>
        /// Habilitar limpieza automática de caracteres especiales
        /// </summary>
        public bool EnableCharacterCleaning { get; set; } = true;

        /// <summary>
        /// Habilitar reemplazo de punto decimal por coma
        /// </summary>
        public bool EnableDecimalPointReplacement { get; set; } = true;

        /// <summary>
        /// Habilitar eliminación de líneas vacías al final
        /// </summary>
        public bool EnableEmptyLinesRemoval { get; set; } = true;
    }
}
