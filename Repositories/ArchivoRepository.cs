using Dapper;
using System.Data;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Validaciones;

namespace SeguridadSocialApi.Repositories
{
    public class ArchivoRepository : IArchivoRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ValidacionExecutor _validacionExecutor;
        private readonly IFlowSessionManager _flowSessionManager;

        public ArchivoRepository(
       IUnitOfWork unitOfWork,
   ValidacionExecutor validacionExecutor,
  IFlowSessionManager flowSessionManager)
        {
            _unitOfWork = unitOfWork;
            _validacionExecutor = validacionExecutor;
            _flowSessionManager = flowSessionManager;
        }

        public async Task<long> GetNextArchivoSeqAsync()
        {
            try
            {
                var sql = "SELECT SQL_LOADER.ARCHIVO_SEQ.NEXTVAL FROM DUAL";
                return await _unitOfWork.Connection.ExecuteScalarAsync<long>(sql);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener secuencia de archivo: {ex.Message}");
            }
        }

        public async Task<int> CrearExtabArchivoAsync(string nombreArchivoServer, long idArchivo, int tipoNovedad, string nombreArchivoHost, string hostName)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("vNOMBREARCHIVOSERVER", nombreArchivoServer, DbType.String);
                parameters.Add("vIDARCHIVO", idArchivo, DbType.Int64);
                parameters.Add("vIDNOVEDAD", tipoNovedad, DbType.Int32);
                parameters.Add("vNOMBREARCHIVOHOST", nombreArchivoHost, DbType.String);
                parameters.Add("vHOSTNAME", hostName, DbType.String);

                return await _unitOfWork.Connection.ExecuteAsync("USUARIO.SQLLDR.CREAR_EXTAB_ARCHIVO",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al crear external table de archivo: {ex.Message}");
            }
        }

        public async Task<(ArchivoInfoDto ArchivoInfo, SumaRemuneracionesDto SumaRemuneraciones)> GetArchivoCargadoInfoConSumasAsync(long idArchivo)
        {
            try
            {
                // Oracle no soporta QueryMultiple con múltiples SELECTs, ejecutamos en paralelo con Task.WhenAll
                var archivoInfoTask = _unitOfWork.Connection.QueryFirstOrDefaultAsync<ArchivoInfoDto>(
                    "SELECT CANTREG, ERROR_ORA as ERRORORA, MENSAJE FROM SQL_LOADER.ARCHIVOS_CARGADOS WHERE IDARCHIVO = :IdArchivo",
                    new { IdArchivo = idArchivo });

                var sumaRemuneracionesTask = _unitOfWork.Connection.QueryFirstOrDefaultAsync<SumaRemuneracionesDto>(
                    @"SELECT SUM(TMP.REMUNIMPONIBLE1) AS SumRem1,
                             SUM(TMP.REMUNIMPONIBLE2) AS SumRem2,
                             SUM(TMP.REMUNIMPONIBLE3) AS SumRem3
                      FROM USUARIO.TMP_NOV_DDJJ_PREV TMP");

                await Task.WhenAll(archivoInfoTask, sumaRemuneracionesTask);

                return (archivoInfoTask.Result ?? new ArchivoInfoDto(),
                        sumaRemuneracionesTask.Result ?? new SumaRemuneracionesDto());
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener información de archivo y sumas: {ex.Message}");
            }
        }

        public async Task<ValidacionArchivoDto> ValidarArchivoAsync(long idArchivo, string? flowId = null)
        {
            try
            {
                IDbConnection connectionToUse = _unitOfWork.Connection;

                // Si viene flowId, usar la conexión fijada para mantener la GTT
                if (!string.IsNullOrWhiteSpace(flowId))
                {
                    var pinnedConnection = _flowSessionManager.GetConnection(flowId);
                    if (pinnedConnection == null)
                    {
                        throw new ApplicationException(
                    $"El flujo '{flowId}' expiró o es inválido. La tabla temporal no está disponible.");
                    }
                    connectionToUse = pinnedConnection;
                }

                // Obtener total de registros en la tabla temporal
                var totalRegistros = await connectionToUse.ExecuteScalarAsync<int>(
                           "SELECT COUNT(*) FROM USUARIO.TMP_NOV_DDJJ_PREV");

                // Si no hay registros y esperábamos encontrar datos
                if (totalRegistros == 0 && !string.IsNullOrWhiteSpace(flowId))
                {
                    throw new ApplicationException(
                       "No se encontraron registros en la tabla temporal. Verifique que el archivo fue cargado correctamente.");
                }

                // Ejecutar todas las validaciones usando el executor
                var resultado = await _validacionExecutor.EjecutarValidacionesAsync(
                    connectionToUse,
                     idArchivo,
                    totalRegistros);

                return resultado;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al validar archivo: {ex.Message}");
            }
        }
    }
}

