using Dapper;
using System.Data;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.Interfaces;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories
{
    public class HojaRepository : IHojaRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public HojaRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<HojasPaginadasDto> GetHojasAsync(DateTime? periodo, int? estado, int? nroHoja, int? idRep, int page = 1, int pageSize = 10)
        {
            try
            {
                var whereConditions = new List<string> { "H.TIPOENTRADA = 156" };
                var parameters = new DynamicParameters();

                if (periodo.HasValue)
                {
                    whereConditions.Add("H.PERIODO = :Periodo");
                    parameters.Add("Periodo", periodo.Value, DbType.DateTime);
                }

                if (estado.HasValue)
                {
                    whereConditions.Add("H.IDESTADO = :Estado");
                    parameters.Add("Estado", estado.Value, DbType.Int32);
                }

                if (nroHoja.HasValue)
                {
                    whereConditions.Add("MOD(H.ID,10000) = :NroHoja");
                    parameters.Add("NroHoja", nroHoja.Value, DbType.Int32);
                }

                if (idRep.HasValue)
                {
                    whereConditions.Add("TO_NUMBER(TRIM(H.OBSERVACIONES)) = :IdRep");
                    parameters.Add("IdRep", idRep.Value, DbType.Int32);
                }

                var whereClause = string.Join(" AND ", whereConditions);

                // Primero obtener el conteo total de registros (sin paginación)
                var countSql = $@"SELECT COUNT(*) FROM USUARIO.HOJA H WHERE {whereClause}";
                var totalRegistros = await _unitOfWork.Connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Luego obtener los registros paginados
                var sql = $@"SELECT h.ID,
                                    MOD(h.ID,10000) as NROHOJA,
                                    H.PERIODO,
                                    H.IDTIPOLIQUIDACION,
                                    TL.DESCRIPCION AS TIPOLIQUIDACION,
                                    H.IDGRUPOADICIONAL,
                                    H.IDESTADO,
                                    E.DESCRIPCION AS ESTADO,
                                    H.FECHAALTA,
                                    H.CANTIDADREG,
                                    TO_NUMBER(TRIM(H.OBSERVACIONES)) AS IDREP
                             FROM USUARIO.HOJA H
                             INNER JOIN USUARIO.ESTADO E ON H.IDESTADO = E.IDESTADO
                             INNER JOIN USUARIO.TABTIPOLIQUIDACION TL ON TL.IDTIPOLIQUIDACION = H.IDTIPOLIQUIDACION
                             WHERE {whereClause}";

                // Agregar paginación with ROWNUM
                parameters.Add("PageSize", pageSize, DbType.Int32);
                parameters.Add("Page", page, DbType.Int32);

                var paginatedSql = $@"
                    SELECT *
                    FROM (SELECT a.*, ROWNUM rnum
                            FROM ({sql}) a
                            WHERE ROWNUM <= :PageSize * :Page) b
                    WHERE rnum > :PageSize * (:Page - 1)
                ";

                var hojas = await _unitOfWork.Connection.QueryAsync<HojaDto>(paginatedSql, parameters);

                return new HojasPaginadasDto
                {
                    TotalRegistros = totalRegistros,
                    Hojas = hojas.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener hojas: {ex.Message}");
            }
        }

        public async Task<int> CrearHojaAsync(long idArchivo, int tipoNovedad, int grupoAdicional, int tipoLiquidacion, int cantidadRegistros, DateTime periodo, int idRep)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("vTIPOENTRADA", tipoNovedad, DbType.Int32);
                parameters.Add("vGRUPOADIC", grupoAdicional, DbType.Int32);
                parameters.Add("vTIPOLIQ", tipoLiquidacion, DbType.Int32);
                parameters.Add("vCANTREG", cantidadRegistros, DbType.Int32);
                parameters.Add("vIDARCHIVO", idArchivo, DbType.Int64);
                parameters.Add("vPERIODO", periodo, DbType.DateTime);
                parameters.Add("vIDREP", idRep, DbType.Int32);
                parameters.Add("vNROHOJA", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _unitOfWork.Connection.ExecuteAsync("USUARIO.MOD_TEMPORALES.W_INSERT_NOVDDJJPREV",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return parameters.Get<int>("vNROHOJA");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al crear hoja: {ex.Message}");
            }
        }

        public async Task ProcesarHojaAsync(int nroHoja)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("vNROHOJA", nroHoja, DbType.Int32, ParameterDirection.Input);

                await _unitOfWork.Connection.ExecuteAsync(
                    "SEGSOCIAL.DDJJ_MENSUAL.CARGA_DDJJ_MENSUAL_REP",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al procesar hoja {nroHoja}: {ex.Message}");
            }
        }

        public async Task AnularHojaAsync(int nroHoja)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("vNrohoja", nroHoja, DbType.Int32, ParameterDirection.Input);

                await _unitOfWork.Connection.ExecuteAsync(
                    "USUARIO.WORKFLOW.hoja_anular",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al anular hoja {nroHoja}: {ex.Message}");
            }
        }
    }
}

