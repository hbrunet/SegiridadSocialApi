// <copyright file="IArchivoRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using SeguridadSocialApi.Services.DTOs;

namespace SeguridadSocialApi.Repositories;

public interface IArchivoRepository
{
    Task<long> GetNextArchivoSeqAsync();

    Task<int> CrearExtabArchivoAsync(string nombreArchivoServer, long idArchivo, int tipoNovedad, string nombreArchivoHost, string hostName);

    Task<(ArchivoInfoDto ArchivoInfo, SumaRemuneracionesDto SumaRemuneraciones)> GetArchivoCargadoInfoConSumasAsync(long idArchivo);

    /// <summary>
    /// Valida un archivo usando su FlowId para mantener la sesión de Oracle.
    /// </summary>
    /// <param name="idArchivo">ID del archivo a validar.</param>
    /// <param name="flowId">ID del flujo para reutilizar la sesión Oracle con la GTT.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<ValidacionArchivoDto> ValidarArchivoAsync(long idArchivo, string? flowId = null);
}
