
using System.Net;
using System.Text;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Controllers.Requests;
using SeguridadSocialApi.Controllers.Responses;
using SeguridadSocialApi.Repositories;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Services
{
    public class NovedadesService
    {
    private readonly IArchivoRepository _archivoRepository;
    private readonly IHojaRepository _hojaRepository;
    private readonly IConfiguration _config;
    private readonly FtpService _ftpService;
    private readonly IFlowSessionManager _flowSessionManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobManager _jobManager;
    private readonly BackgroundJobExecutor _jobExecutor;
    private readonly IJobProgressRepository _jobProgressRepository;
        public async Task<HojasPaginadasDto> GetHojasAsync(DateTime? periodo, int? estado, int? nroHoja, int? idRep, int page = 1, int pageSize = 10)
        {
            return await _hojaRepository.GetHojasAsync(periodo, estado, nroHoja, idRep, page, pageSize);
        }

        public NovedadesService(IArchivoRepository archivoRepository, IHojaRepository hojaRepository, IConfiguration config, FtpService ftpService, IFlowSessionManager flowSessionManager, IUnitOfWork unitOfWork, IJobManager jobManager, BackgroundJobExecutor jobExecutor, IJobProgressRepository jobProgressRepository)
        {
            _archivoRepository = archivoRepository;
            _hojaRepository = hojaRepository;
            _config = config;
            _ftpService = ftpService;
            _flowSessionManager = flowSessionManager;
            _unitOfWork = unitOfWork;
            _jobManager = jobManager;
            _jobExecutor = jobExecutor;
            _jobProgressRepository = jobProgressRepository;
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file, int tipoNovedad)
        {
            // Iniciar un flujo para mantener la misma sesión de Oracle entre upload y creación
            string? flowId = null;
            OracleConnection? pinnedConn = null;
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var filePath = Path.Combine(uploads, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Normalizar el archivo a UTF-8 sin BOM para compatibilidad con Oracle AL32UTF8
            await NormalizarArchivoAUtf8(filePath);

            var idArchivo = await _archivoRepository.GetNextArchivoSeqAsync();
            var nombreArchivoServer = $"{idArchivo}_{file.FileName.Substring(0, 3)}.TXT";

            await _ftpService.UploadFileAsync(filePath, nombreArchivoServer);

            try
            {
                flowId = await _flowSessionManager.StartAsync();
                pinnedConn = _flowSessionManager.GetConnection(flowId);
                if (pinnedConn == null)
                    throw new ApplicationException("No se pudo inicializar la sesión de base de datos para el flujo.");

                // Hacer que los repos usen la conexión fijada a la sesión
                _unitOfWork.UseExternalConnection(pinnedConn, ownsConnection: false);

                await _archivoRepository.CrearExtabArchivoAsync(nombreArchivoServer, idArchivo, tipoNovedad, file.FileName, Dns.GetHostName());
                
                // Usar QueryMultiple para obtener info del archivo y sumas en un solo roundtrip
                var (archivoInfo, sumaRemuneraciones) = await _archivoRepository.GetArchivoCargadoInfoConSumasAsync(idArchivo);

                return new UploadResponse
                {
                    FileName = file.FileName,
                    CantidadRegistros = archivoInfo.CantReg,
                    ErrorOra = archivoInfo.ErrorOra,
                    Mensaje = archivoInfo.Mensaje ?? string.Empty,
                    IdArchivo = idArchivo,
                    SumRem1 = sumaRemuneraciones.SumRem1,
                    SumRem2 = sumaRemuneraciones.SumRem2,
                    SumRem3 = sumaRemuneraciones.SumRem3,
                    FlowId = flowId
                };
            }
            catch
            {
                if (!string.IsNullOrEmpty(flowId))
                {
                    await _flowSessionManager.EndAsync(flowId);
                }
                throw;
            }
        }

        public async Task<CrearHojaResponse> CrearHojaAsync(CrearHojaRequest request)
        {
            // Si viene un flowId, reutilizamos la misma sesión Oracle para que la GTT esté disponible
            if (!string.IsNullOrWhiteSpace(request.FlowId))
            {
                var conn = _flowSessionManager.GetConnection(request.FlowId);
                if (conn == null)
                {
                    throw new ApplicationException("El flujo indicado expiró o es inválido. Vuelva a cargar el archivo.");
                }
                _unitOfWork.UseExternalConnection(conn, ownsConnection: false);
            }

            var nroHoja = await _hojaRepository.CrearHojaAsync(
                request.IdArchivo,
                request.TipoNovedad,
                request.GrupoAdicional,
                request.TipoLiquidacion,
                request.CantidadRegistros,
                request.Periodo,
                request.IdRep
            );

            // Si hubo flow, cerrar el flujo y su conexión
            if (!string.IsNullOrWhiteSpace(request.FlowId))
            {
                await _flowSessionManager.EndAsync(request.FlowId!);
            }

            return new CrearHojaResponse
            {
                IdArchivo = request.IdArchivo,
                TipoNovedad = request.TipoNovedad,
                GrupoAdicional = request.GrupoAdicional,
                TipoLiquidacion = request.TipoLiquidacion,
                NroHoja = nroHoja
            };
        }

        /// <summary>
        /// Inicia la creación de hoja como job en background para SPs de larga duración.
        /// El progreso se obtiene consultando una tabla de Oracle donde el SP reporta su avance.
        /// </summary>
        public StartJobResponse CrearHojaAsyncJob(CrearHojaRequest request)
        {
            // Generar jobId antes para pasarlo al SP
            var capturedJobId = Guid.NewGuid().ToString("N");
            
            var jobId = _jobManager.CreateJob(
                async (progress, cancellationToken) =>
                {
                    // Si viene flowId, reutilizar sesión
                    if (!string.IsNullOrWhiteSpace(request.FlowId))
                    {
                        var conn = _flowSessionManager.GetConnection(request.FlowId);
                        if (conn == null)
                        {
                            throw new ApplicationException("El flujo indicado expiró o es inválido. Vuelva a cargar el archivo.");
                        }
                        _unitOfWork.UseExternalConnection(conn, ownsConnection: false);
                    }

                    // Inicializar registro de progreso en Oracle
                    await _jobProgressRepository.InitializeAsync(capturedJobId);

                    // Iniciar polling de progreso en background
                    var progressPollingTask = Task.Run(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                var (progressPct, statusMsg) = await _jobProgressRepository.GetProgressAsync(capturedJobId);
                                // Actualizar porcentaje y mensaje para que el frontend lo vea en JobInfoDto
                                _jobManager.UpdateProgress(capturedJobId, progressPct, statusMsg);
                                
                                // Consultar cada 2 segundos
                                await Task.Delay(2000, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                // Ignorar errores de polling, el SP sigue ejecutándose
                                System.Diagnostics.Debug.WriteLine($"Error polling progreso: {ex.Message}");
                            }
                        }
                    }, cancellationToken);

                    // Ejecutar SP (el SP actualiza su progreso en la tabla USUARIO.JOB_PROGRESS)
                    var nroHoja = await _hojaRepository.CrearHojaAsync(
                        request.IdArchivo,
                        request.TipoNovedad,
                        request.GrupoAdicional,
                        request.TipoLiquidacion,
                        request.CantidadRegistros,
                        request.Periodo,
                        request.IdRep
                    );

                    // Detener polling
                    try
                    {
                        await progressPollingTask;
                    }
                    catch { /* Ya completado */ }

                    // Si hubo flow, cerrar
                    if (!string.IsNullOrWhiteSpace(request.FlowId))
                    {
                        await _flowSessionManager.EndAsync(request.FlowId!);
                    }

                    // Limpiar tabla de progreso
                    await _jobProgressRepository.DeleteAsync(capturedJobId);

                    return new CrearHojaResponse
                    {
                        IdArchivo = request.IdArchivo,
                        TipoNovedad = request.TipoNovedad,
                        GrupoAdicional = request.GrupoAdicional,
                        TipoLiquidacion = request.TipoLiquidacion,
                        NroHoja = nroHoja
                    };
                },
                $"Crear hoja para archivo {request.IdArchivo}"
            );

            // Encolar el job para ejecución
            _jobExecutor.EnqueueJob(jobId);

            return new StartJobResponse
            {
                JobId = jobId,
                Message = "Creación de hoja iniciada en background. Use el jobId para consultar el estado."
            };
        }

        /// <summary>
        /// Normaliza el archivo a UTF-8 sin BOM, detectando automáticamente el encoding original.
        /// Soporta: UTF-8 (con/sin BOM), Windows-1252/ANSI, ISO-8859-1.
        /// </summary>
        private async Task NormalizarArchivoAUtf8(string filePath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                
                // Detectar encoding original
                Encoding encodingOriginal;
                int startIndex = 0;

                // Verificar BOM UTF-8 (EF BB BF)
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    encodingOriginal = Encoding.UTF8;
                    startIndex = 3; // Saltar BOM para removerlo
                }
                // Verificar BOM UTF-16 LE (FF FE)
                else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                {
                    encodingOriginal = Encoding.Unicode;
                    startIndex = 2;
                }
                // Verificar BOM UTF-16 BE (FE FF)
                else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                {
                    encodingOriginal = Encoding.BigEndianUnicode;
                    startIndex = 2;
                }
                // Sin BOM: detectar por heurística (ANSI/Windows-1252 es el más común en Windows)
                else
                {
                    // Intentar detectar si es UTF-8 válido sin BOM
                    try
                    {
                        var utf8Decoder = Encoding.UTF8.GetDecoder();
                        utf8Decoder.GetCharCount(bytes, 0, bytes.Length, true);
                        encodingOriginal = Encoding.UTF8;
                    }
                    catch (DecoderFallbackException)
                    {
                        // No es UTF-8 válido, asumir Windows-1252 (ANSI Latin 1)
                        encodingOriginal = Encoding.GetEncoding(1252);
                    }
                }

                // Leer contenido con el encoding detectado
                string contenido;
                if (startIndex > 0)
                {
                    var bytesContenido = new byte[bytes.Length - startIndex];
                    Array.Copy(bytes, startIndex, bytesContenido, 0, bytesContenido.Length);
                    contenido = encodingOriginal.GetString(bytesContenido);
                }
                else
                {
                    contenido = encodingOriginal.GetString(bytes);
                }

                // Escribir como UTF-8 sin BOM
                await File.WriteAllTextAsync(filePath, contenido, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al normalizar encoding del archivo: {ex.Message}");
            }
        }

        // Progreso Oracle encapsulado en IJobProgressRepository
    }
}