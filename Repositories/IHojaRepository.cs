using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories
{
    public interface IHojaRepository
    {
        Task<HojasPaginadasDto> GetHojasAsync(DateTime? periodo, int? estado, int? nroHoja, int? idRep, int page = 1, int pageSize = 10);
        Task<int> CrearHojaAsync(long idArchivo, int tipoNovedad, int grupoAdicional, int tipoLiquidacion, int cantidadRegistros, DateTime periodo, int idRep);
        Task ProcesarHojaAsync(int nroHoja);
        Task AnularHojaAsync(int nroHoja);
    }
}
