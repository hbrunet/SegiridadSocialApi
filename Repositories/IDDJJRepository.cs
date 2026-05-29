using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories;

public interface IDDJJRepository
{
    /// <summary>
    /// Orquesta la unificación de datos de las declaraciones juradas de un periodo específico.
    /// </summary>
    /// <param name="connection">Conexión Oracle a utilizar.</param>
    /// <param name="periodo">Periodo a procesar.</param>
    /// <param name="cuil">CUIL del empleador/trabajador a procesar.</param>
    /// <param name="jobId">ID del job para tracking (opcional).</param>
    Task FusionarDatosAsync(System.Data.IDbConnection connection, DateTime periodo, long? cuil = null, string? jobId = null);

    /// <summary>
    /// Exporta las líneas de presentación de un periodo desde la vista Oracle hacia un stream de salida.
    /// </summary>
    /// <param name="connection">Conexión Oracle a utilizar.</param>
    /// <param name="periodo">Periodo a exportar.</param>
    /// <param name="outputStream">Stream de destino donde se escribirán las líneas del archivo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de registros escritos.</returns>
    Task<int> ExportarPresentacionAsync(System.Data.IDbConnection connection, DateTime periodo, Stream outputStream, CancellationToken cancellationToken = default);
}
