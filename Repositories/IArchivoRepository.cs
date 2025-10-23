using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories
{
    public interface IArchivoRepository
    {
        Task<long> GetNextArchivoSeqAsync();
        Task<int> CrearExtabArchivoAsync(string nombreArchivoServer, long idArchivo, int tipoNovedad, string nombreArchivoHost, string hostName);
        Task<(ArchivoInfoDto ArchivoInfo, SumaRemuneracionesDto SumaRemuneraciones)> GetArchivoCargadoInfoConSumasAsync(long idArchivo);
    }
}
