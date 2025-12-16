// <copyright file="IJobProgressRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace SeguridadSocialApi.Repositories;

public interface IJobProgressRepository
{
    Task InitializeAsync(string jobId, string initialMessage = "Iniciando...");

    Task<(int progressPct, string statusMessage)> GetProgressAsync(string jobId);

    Task UpdateAsync(string jobId, int progressPct, string statusMessage);

    Task DeleteAsync(string jobId);
}
