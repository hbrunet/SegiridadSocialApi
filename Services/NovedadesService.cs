// <copyright file="NovedadesService.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Net;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Common;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.Options;
using Serilog;

namespace SeguridadSocialApi.Services;

public class NovedadesService
{
    private readonly IArchivoRepository archivoRepository;
    private readonly IHojaRepository hojaRepository;
    private readonly FtpService ftpService;
    private readonly IFlowSessionManager flowSessionManager;
    private readonly IUnitOfWork unitOfWork;
    private readonly IFileStorageService fileStorage;
    private readonly IFileNormalizationService fileNormalization;
    private readonly FileUploadOptions uploadOptions;
    private readonly IJobProgressRepository jobProgressRepository;

    public NovedadesService(
 IArchivoRepository archivoRepository,
 IHojaRepository hojaRepository,
 FtpService ftpService,
 IFlowSessionManager flowSessionManager,
 IUnitOfWork unitOfWork,
 IFileStorageService fileStorage,
 IFileNormalizationService fileNormalization,
 IOptions<FileUploadOptions> uploadOptions,
 IJobProgressRepository jobProgressRepository)
    {
        this.archivoRepository = archivoRepository;
        this.hojaRepository = hojaRepository;
        this.ftpService = ftpService;
        this.flowSessionManager = flowSessionManager;
        this.unitOfWork = unitOfWork;
        this.fileStorage = fileStorage;
        this.fileNormalization = fileNormalization;
        this.uploadOptions = uploadOptions.Value;
    }

    public async Task<HojasPaginadasDto> GetHojasAsync(DateTime? periodo, int? estado, int? nroHoja, int? idRep, int page = 1, int pageSize = 10)
    {
        return await hojaRepository.GetHojasAsync(periodo, estado, nroHoja, idRep, page, pageSize);
    }

    public async Task<UploadResponse> UploadFileAsync(IFormFile file, int tipoNovedad, bool autoValidar = false)
    {
        string? flowId = null;
        string? filePath = null;

        try
        {
            // 1. Guardar archivo con validación automática
            var saveResult = await fileStorage.SaveUploadedFileAsync(file);
            if (!saveResult.IsSuccess)
            {
                Log.Error("Error al guardar archivo: {Error}", saveResult.Error);
                throw new ApplicationException(saveResult.Error!);
            }

            filePath = saveResult.Value!;
            Log.Information("Archivo guardado exitosamente en {FilePath}", filePath);

            // 2. Normalizar encoding y contenido si está habilitado
            if (uploadOptions.EnableEncodingNormalization)
            {
                var normalizeResult = await fileNormalization.NormalizeFileAsync(filePath);
                if (!normalizeResult.IsSuccess)
                {
                    Log.Error("Error al normalizar archivo: {Error}", normalizeResult.Error);
                    throw new ApplicationException(normalizeResult.Error!);
                }
            }

            // 3. Generar nombre para servidor y subir a FTP
            var idArchivo = await archivoRepository.GetNextArchivoSeqAsync();
            var nombreArchivoServer = GenerateServerFileName(file.FileName, idArchivo);

            await ftpService.UploadFileAsync(filePath, nombreArchivoServer);
            Log.Information("Archivo subido a FTP: {ServerFileName}", nombreArchivoServer);

            // 4. Iniciar flujo de sesión Oracle
            flowId = await flowSessionManager.StartAsync();
            var pinnedConn = flowSessionManager.GetConnection(flowId);

            if (pinnedConn == null)
            {
                throw new ApplicationException("No se pudo inicializar la sesión de base de datos para el flujo.");
            }

            unitOfWork.UseExternalConnection(pinnedConn, ownsConnection: false);

            // 5. Crear external table en Oracle
            await archivoRepository.CrearExtabArchivoAsync(
                nombreArchivoServer,
                idArchivo,
                tipoNovedad,
                file.FileName,
                Dns.GetHostName());

            // 6. Obtener información del archivo cargado
            var (archivoInfo, sumaRemuneraciones) = await archivoRepository.GetArchivoCargadoInfoConSumasAsync(idArchivo);

            var response = new UploadResponse
            {
                FileName = file.FileName,
                CantidadRegistros = archivoInfo.CantReg,
                ErrorOra = archivoInfo.ErrorOra,
                Mensaje = archivoInfo.Mensaje ?? string.Empty,
                IdArchivo = idArchivo,
                SumRem1 = sumaRemuneraciones.SumRem1,
                SumRem2 = sumaRemuneraciones.SumRem2,
                SumRem3 = sumaRemuneraciones.SumRem3,
                FlowId = flowId,
            };

            // 7. Auto-validación si está habilitada
            if (autoValidar && archivoInfo.CantReg > 0)
            {
                try
                {
                    var validaciones = await archivoRepository.ValidarArchivoAsync(idArchivo, flowId);
                    response.Validaciones = validaciones;
                    Log.Information("Validaciones automáticas completadas para archivo {IdArchivo}", idArchivo);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error al ejecutar validaciones automáticas para archivo {IdArchivo}", idArchivo);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            // Limpiar recursos en caso de error
            if (!string.IsNullOrEmpty(flowId))
            {
                await flowSessionManager.EndAsync(flowId);
            }

            // Intentar eliminar archivo temporal si existe
            if (!string.IsNullOrEmpty(filePath) && fileStorage.FileExists(filePath))
            {
                await fileStorage.DeleteFileAsync(filePath);
            }

            Log.Error(ex, "Error en UploadFileAsync para archivo {FileName}", file?.FileName);
            throw;
        }
    }

    public async Task<CrearHojaResponse> CrearHojaAsync(CrearHojaRequest request)
    {
        // Si viene un flowId, reutilizamos la misma sesión Oracle para que la GTT esté disponible
        if (!string.IsNullOrWhiteSpace(request.FlowId))
        {
            var conn = flowSessionManager.GetConnection(request.FlowId);
            if (conn == null)
            {
                throw new ApplicationException("El flujo indicado expiró o es inválido. Vuelva a cargar el archivo.");
            }

            unitOfWork.UseExternalConnection(conn, ownsConnection: false);
        }

        var nroHoja = await hojaRepository.CrearHojaAsync(
            request.IdArchivo,
            request.TipoNovedad,
            request.GrupoAdicional,
            request.TipoLiquidacion,
            request.CantidadRegistros,
            request.Periodo,
            request.IdRep);

        // Si hubo flow, cerrar el flujo y su conexión
        if (!string.IsNullOrWhiteSpace(request.FlowId))
        {
            await flowSessionManager.EndAsync(request.FlowId!);
            Log.Information("Flujo de sesión finalizado: {FlowId}", request.FlowId);
        }

        return new CrearHojaResponse
        {
            IdArchivo = request.IdArchivo,
            TipoNovedad = request.TipoNovedad,
            GrupoAdicional = request.GrupoAdicional,
            TipoLiquidacion = request.TipoLiquidacion,
            NroHoja = nroHoja,
        };
    }

    public async Task ProcesarHojaAsync(int nroHoja)
    {
        await hojaRepository.ProcesarHojaAsync(nroHoja);
        Log.Information("Hoja procesada: {NroHoja}", nroHoja);
    }

    public async Task AnularHojaAsync(int nroHoja)
    {
        await hojaRepository.AnularHojaAsync(nroHoja);
        Log.Information("Hoja anulada: {NroHoja}", nroHoja);
    }

    public async Task<ValidacionArchivoDto> ValidarArchivoAsync(int idArchivo, string? flowId = null)
    {
        return await archivoRepository.ValidarArchivoAsync(idArchivo, flowId);
    }

    /// <summary>
    /// Genera el nombre de archivo para el servidor FTP.
    /// </summary>
    private string GenerateServerFileName(string originalFileName, long idArchivo)
    {
        var prefix = originalFileName.Length >= uploadOptions.FileNamePrefixLength
         ? originalFileName.Substring(0, uploadOptions.FileNamePrefixLength)
             : originalFileName;

        return $"{idArchivo}_{prefix}.TXT";
    }
}
