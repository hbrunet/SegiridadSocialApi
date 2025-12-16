// <copyright file="FtpService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using FluentFTP;

namespace SeguridadSocialApi.Services;

public class FtpService
{
    private readonly string _ftpHost;
    private readonly string ftpFolder;
    private readonly IConfiguration config;

    public FtpService(IConfiguration config)
    {
        this.config = config;
        _ftpHost = this.config["FtpConfig:Host"] ?? throw new InvalidOperationException("FtpConfig:Host no configurado en appsettings.json");
        ftpFolder = this.config["FtpConfig:RemoteFolder"] ?? throw new InvalidOperationException("FtpConfig:RemoteFolder no configurado en appsettings.json");
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
            var remotePath = $"/{ftpFolder}/{fileName}";

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
