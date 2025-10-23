using Dapper;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories
{
    public class ConfiguracionRepository : IConfiguracionRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConfiguracionRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TipoHojaDto>> GetTiposHojaExternosAsync()
        {
            try
            {
                var sql = @"SELECT TH.ID, TH.NOMBRE, TH.DESCRIPCION, TH.PROCVALIDADOR, TH.PROCTRANSFORMADOR
                            FROM USUARIO.TABTIPOHOJA TH
                            INNER JOIN USUARIO.RELACION_TIPOMEDIO TM ON TH.ID = TM.IDNOV
                            WHERE TM.EXTERNA = 1
                            ORDER BY 1";

                var result = await _unitOfWork.Connection.QueryAsync<TipoHojaDto>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener tipos de hoja: {ex.Message}");
            }
        }

        public async Task<List<GrupoAdicionalDto>> GetGruposAdicionalesAsync()
        {
            try
            {
                var sql = @"SELECT GA.IDGRUPO, GA.DESCRIPCION
                            FROM USUARIO.GRUPOADICIONAL GA
                            WHERE IDESTADO = 1
                            ORDER BY 1";

                var result = await _unitOfWork.Connection.QueryAsync<GrupoAdicionalDto>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener grupos adicionales: {ex.Message}");
            }
        }

        public async Task<List<TipoLiquidacionDto>> GetTiposLiquidacionAsync()
        {
            try
            {
                var sql = @"SELECT TL.IDTIPOLIQUIDACION, TL.DESCRIPCION
                            FROM USUARIO.TABTIPOLIQUIDACION TL
                            ORDER BY 1";

                var result = await _unitOfWork.Connection.QueryAsync<TipoLiquidacionDto>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener tipos de liquidación: {ex.Message}");
            }
        }

        public async Task<List<ReparticionDto>> GetReparticionesSegSocialAsync()
        {
            try
            {
                var sql = "SELECT IDREP, DESCRIPCION FROM SEGSOCIAL.REPARTICION";

                var result = await _unitOfWork.Connection.QueryAsync<ReparticionDto>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener reparticiones: {ex.Message}");
            }
        }

        public async Task<List<EstadoDto>> GetEstadosHojaAsync()
        {
            try
            {
                var sql = "SELECT * FROM USUARIO.ESTADO";
                var result = await _unitOfWork.Connection.QueryAsync<EstadoDto>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al obtener estados: {ex.Message}");
            }
        }
    }
}
