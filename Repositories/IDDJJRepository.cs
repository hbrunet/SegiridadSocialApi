using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories;

public interface IDDJJRepository
{
    /// <summary>
    /// Fusiona los datos de las declaraciones juradas de un periodo específico.
    /// </summary>
    /// <param name="connection">Conexión Oracle a utilizar.</param>
    /// <param name="periodo">Periodo a fusionar.</param>
    /// <param name="jobId">ID del job para tracking (opcional).</param>
    Task FusionarDatosAsync(System.Data.IDbConnection connection, DateTime periodo, string? jobId = null);
}
