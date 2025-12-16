// <copyright file="DDJJRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Dapper;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Repositories;

public class DDJJRepository : IDDJJRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public DDJJRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task FusionarDatosAsync(IDbConnection connection, DateTime periodo, string? jobId = null)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_periodo", periodo, DbType.DateTime, ParameterDirection.Input);
            parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);

            await connection.ExecuteAsync(
                "SEGSOCIAL.PKG_DDJJ_FUSION.PROCESAR_PERIODO",
                parameters,
                commandType: CommandType.StoredProcedure); 
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al fusionar datos del periodo {periodo:yyyy-MM}: {ex.Message}", ex);
        }
    }
}
