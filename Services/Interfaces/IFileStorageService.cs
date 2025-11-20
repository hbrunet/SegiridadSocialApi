using SeguridadSocialApi.Common;

namespace SeguridadSocialApi.Services.Interfaces
{
    /// <summary>
    /// Servicio para operaciones de almacenamiento de archivos
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda un archivo subido en el almacenamiento local
        /// </summary>
        Task<Result<string>> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lee el contenido de un archivo como bytes
        /// </summary>
        Task<Result<byte[]>> ReadFileBytesAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Escribe contenido a un archivo
        /// </summary>
        Task<Result<string>> WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un archivo del almacenamiento
        /// </summary>
        Task<Result> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        bool FileExists(string filePath);

        /// <summary>
        /// Obtiene el directorio de uploads configurado
        /// </summary>
        string GetUploadDirectory();
    }
}
