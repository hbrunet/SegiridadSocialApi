using FluentFTP;

namespace SeguridadSocialApi.Services
{
    public class FtpService
    {
        private readonly string _ftpHost;
        private readonly string _ftpFolder;
        private readonly IConfiguration _config;

        public FtpService(IConfiguration config)
        {
            _config = config;
            _ftpHost = _config["FtpConfig:Host"] ?? throw new InvalidOperationException("FtpConfig:Host no configurado en appsettings.json");
            _ftpFolder = _config["FtpConfig:RemoteFolder"] ?? throw new InvalidOperationException("FtpConfig:RemoteFolder no configurado en appsettings.json");
        }

        public async Task UploadFileAsync(string localFilePath, string fileName)
        {
            try
            {
                // Crear cliente FTP asíncrono
                using var client = new AsyncFtpClient(_ftpHost);
                
                // Configuración equivalente a FtpWebRequest anterior
                client.Config.EncryptionMode = FtpEncryptionMode.None; // EnableSsl = false
                client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                
                // Conectar al servidor FTP (sin credenciales = anónimo)
                await client.Connect();

                // Construir ruta remota completa
                var remotePath = $"/{_ftpFolder}/{fileName}";

                // Subir archivo (equivalente a UploadFile con UseBinary = true)
                await client.UploadFile(localFilePath, remotePath, FtpRemoteExists.Overwrite);

                // Desconectar
                await client.Disconnect();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error al subir el archivo por FTP.", ex);
            }
        }
    }
}
