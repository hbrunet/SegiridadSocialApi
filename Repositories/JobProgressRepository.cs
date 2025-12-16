// <copyright file="JobProgressRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Services;
using SeguridadSocialApi.Services.DTOs;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Repositories;

public class JobProgressRepository : IJobProgressRepository
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IConfiguration configuration;

    public JobProgressRepository(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        this.unitOfWork = unitOfWork;
        this.configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(string jobId, string initialMessage = "Iniciando...")
    {
        const string sql = @"
                                MERGE INTO SEGSOCIAL.JOB_PROGRESS t
                                USING (SELECT :JobId AS JOB_ID FROM dual) s
                                ON (t.JOB_ID = s.JOB_ID)
                                WHEN MATCHED THEN UPDATE SET t.PROGRESS_PCT = 0, t.STATUS_MESSAGE = :Msg, t.LAST_UPDATE = SYSTIMESTAMP
                                WHEN NOT MATCHED THEN INSERT (JOB_ID, PROGRESS_PCT, STATUS_MESSAGE, LAST_UPDATE) VALUES (:JobId, 0, :Msg, SYSTIMESTAMP)";

        await unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId, Msg = initialMessage });

        // Verificar que se creó
        const string verifySql = @"SELECT JOB_ID, PROGRESS_PCT, STATUS_MESSAGE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = :JobId";
        var verifyResult = await unitOfWork.Connection.QueryFirstOrDefaultAsync<JobProgressDto>(verifySql, new { JobId = jobId });

        if (verifyResult != null)
        {
            Console.WriteLine($"? [InitializeAsync] VERIFICADO - Registro creado: pct={verifyResult.PROGRESS_PCT}, msg='{verifyResult.STATUS_MESSAGE}'");
        }
        else
        {
            Console.WriteLine($"?? [InitializeAsync] ADVERTENCIA - Registro NO encontrado después del MERGE!");
        }
    }

    /// <inheritdoc/>
    public async Task<(int progressPct, string statusMessage)> GetProgressAsync(string jobId)
    {
        const string sql = @"SELECT NVL(PROGRESS_PCT,0) AS PROGRESS_PCT, NVL(STATUS_MESSAGE,'') AS STATUS_MESSAGE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = :JobId";

        // Obtener connection string desde configuración (incluye contraseña)
        var connectionString = configuration["OracleConfig:ConnectionString"]
  ?? throw new InvalidOperationException("Oracle connection string not configured");

        // Crear una nueva conexión para no bloquear la conexión del SP
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        // Usar DTO fuertemente tipado para evitar problemas de conversión con Oracle
        var result = await connection.QueryFirstOrDefaultAsync<JobProgressDto>(sql, new { JobId = jobId });

        if (result == null)
        {
            return (0, "No progress record found");
        }

        return (result.PROGRESS_PCT, result.STATUS_MESSAGE);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(string jobId, int progressPct, string statusMessage)
    {
        const string sql = @"UPDATE SEGSOCIAL.JOB_PROGRESS SET PROGRESS_PCT = :Pct, STATUS_MESSAGE = :Msg, LAST_UPDATE = SYSTIMESTAMP WHERE JOB_ID = :JobId";
        await unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId, Pct = progressPct, Msg = statusMessage });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string jobId)
    {
        const string sql = @"DELETE FROM SEGSOCIAL.JOB_PROGRESS WHERE JOB_ID = :JobId";
        await unitOfWork.Connection.ExecuteAsync(sql, new { JobId = jobId });
    }
}
