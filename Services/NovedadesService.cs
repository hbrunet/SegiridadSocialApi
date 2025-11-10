
using System.Net;
using System.Text;
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
        public async Task<HojasPaginadasDto> GetHojasAsync(DateTime? periodo, int? estado, int? nroHoja, int? idRep, int page = 1, int pageSize = 10)
        {
            return await _hojaRepository.GetHojasAsync(periodo, estado, nroHoja, idRep, page, pageSize);
        }

        public NovedadesService(IArchivoRepository archivoRepository, IHojaRepository hojaRepository, IConfiguration config, FtpService ftpService, IFlowSessionManager flowSessionManager, IUnitOfWork unitOfWork)
        {
            _archivoRepository = archivoRepository;
            _hojaRepository = hojaRepository;
            _config = config;
            _ftpService = ftpService;
            _flowSessionManager = flowSessionManager;
            _unitOfWork = unitOfWork;
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

        public async Task ProcesarHojaAsync(int nroHoja)
        {
            await _hojaRepository.ProcesarHojaAsync(nroHoja);
        }

        public async Task AnularHojaAsync(int nroHoja)
        {
            await _hojaRepository.AnularHojaAsync(nroHoja);
        }
    }
}