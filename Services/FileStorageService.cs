using Microsoft.Extensions.Options;
using Serilog;
using SeguridadSocialApi.Common;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;

namespace SeguridadSocialApi.Services
{
    /// <summary>
    /// Implementación del servicio de almacenamiento de archivos
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly FileUploadOptions _options;

        public FileStorageService(IOptions<FileUploadOptions> options)
        {
            _options = options.Value;
            EnsureUploadDirectoryExists();
        }

        public async Task<Result<string>> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Result<string>.Failure("El archivo está vacío o es nulo");

                // Validar tamaño
                if (file.Length > _options.MaxFileSizeBytes)
                    return Result<string>.Failure($"El archivo excede el tamaño máximo permitido de {_options.MaxFileSizeBytes / 1_048_576} MB");

                // Validar extensión
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_options.AllowedExtensions.Contains(extension))
                    return Result<string>.Failure($"Extensión de archivo no permitida. Permitidas: {string.Join(", ", _options.AllowedExtensions)}");

                // Sanitizar nombre de archivo
                var safeFileName = SanitizeFileName(file.FileName);
                var filePath = Path.Combine(GetUploadDirectory(), safeFileName);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                Log.Information("Archivo guardado exitosamente: {FilePath}, Tamaño: {Size} bytes", filePath, file.Length);
                return Result<string>.Success(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al guardar archivo {FileName}", file?.FileName);
                return Result<string>.Failure("Error al guardar el archivo", ex);
            }
        }

        public async Task<Result<byte[]>> ReadFileBytesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                    return Result<byte[]>.Failure($"El archivo no existe: {filePath}");

                var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                Log.Debug("Archivo leído: {FilePath}, Tamaño: {Size} bytes", filePath, bytes.Length);
                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al leer archivo {FilePath}", filePath);
                return Result<byte[]>.Failure("Error al leer el archivo", ex);
            }
        }

        public async Task<Result<string>> WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF8, cancellationToken);
                Log.Debug("Contenido escrito en archivo: {FilePath}, Tamaño: {Size} caracteres", filePath, content.Length);
                return Result<string>.Success(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escribir archivo {FilePath}", filePath);
                return Result<string>.Failure("Error al escribir el archivo", ex);
            }
        }

        public async Task<Result> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath), cancellationToken);
                    Log.Information("Archivo eliminado: {FilePath}", filePath);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al eliminar archivo {FilePath}", filePath);
                return Result.Failure("Error al eliminar el archivo", ex);
            }
        }

        public bool FileExists(string filePath) => File.Exists(filePath);

        public string GetUploadDirectory()
        {
            var uploadDir = Path.IsPathRooted(_options.UploadDirectory)
       ? _options.UploadDirectory
       : Path.Combine(Directory.GetCurrentDirectory(), _options.UploadDirectory);

            return uploadDir;
        }

        private void EnsureUploadDirectoryExists()
        {
            var uploadDir = GetUploadDirectory();
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
                Log.Information("Directorio de uploads creado: {Directory}", uploadDir);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remover caracteres inválidos del nombre de archivo
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Agregar timestamp para evitar colisiones
            var extension = Path.GetExtension(safeName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(safeName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            return $"{nameWithoutExt}_{timestamp}{extension}";
        }
    }
}
