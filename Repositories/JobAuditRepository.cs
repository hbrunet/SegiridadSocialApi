// <copyright file="JobAuditRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Repositories;

public class JobAuditRepository : IJobAuditRepository
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IConfiguration configuration;

    public JobAuditRepository(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        this.unitOfWork = unitOfWork;
        this.configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<long> InsertJobAuditAsync(
                                        string jobId,
                                        string endpoint,
                                        string inputParamsJson,
                                        string? createdBy = null)
    {
        // Para Oracle con parámetro de salida, necesitamos una solución híbrida
        const string sql = @"
                            BEGIN
                            SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
                            :p_job_id,
                            :p_endpoint,
                            :p_input_params,
                            :p_created_by,
                            :p_audit_id
                            );
                            END;";

        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);
        parameters.Add("p_endpoint", endpoint, DbType.String, ParameterDirection.Input);
        parameters.Add("p_input_params", inputParamsJson, DbType.String, ParameterDirection.Input);
        parameters.Add("p_created_by", createdBy, DbType.String, ParameterDirection.Input);
        parameters.Add("p_audit_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

        // Conexión propia para garantizar COMMIT inmediato independiente del UnitOfWork del request HTTP.
        // Sin esto, el INSERT se pierde con el rollback implícito al cerrar la conexión del scope.
        var connectionString = configuration["OracleConfig:ConnectionString"]
            ?? throw new InvalidOperationException("Oracle connection string not configured");

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql, parameters);

        return parameters.Get<long>("p_audit_id");
    }

    /// <inheritdoc/>
    public async Task MarkJobAsStartedAsync(string jobId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);

        await unitOfWork.Connection.ExecuteAsync(
                    "SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_START",
                    parameters,
                    commandType: CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task CompleteJobAuditAsync(
                        string jobId,
                        string status,
                        int progressPct,
                        string? resultDataJson = null,
                        string? errorMessage = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);
        parameters.Add("p_status", status, DbType.String, ParameterDirection.Input);
        parameters.Add("p_progress_pct", progressPct, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_result_data", resultDataJson, DbType.String, ParameterDirection.Input);
        parameters.Add("p_error_message", errorMessage, DbType.String, ParameterDirection.Input);

        await unitOfWork.Connection.ExecuteAsync(
            "SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_COMPLETE",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task AddJobLogAsync(
                                     string jobId,
                                     string logLevel,
                                     string logMessage,
                                     int? progressPct = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);
        parameters.Add("p_log_level", logLevel, DbType.String, ParameterDirection.Input);
        parameters.Add("p_log_message", logMessage, DbType.String, ParameterDirection.Input);
        parameters.Add("p_progress_pct", progressPct, DbType.Int32, ParameterDirection.Input);

        await unitOfWork.Connection.ExecuteAsync(
            "SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_LOG",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task<JobAuditDto?> GetJobAuditAsync(string jobId)
    {
        const string sql = @"
    SELECT 
    JA.AUDIT_ID as AuditId,
JA.JOB_ID as JobId,
   JA.ENDPOINT as Endpoint,
                (SELECT JT.NAME 
              FROM SEGSOCIAL.JOB_TYPE JT 
   WHERE UPPER(JA.ENDPOINT) LIKE '%' || UPPER(JT.NAME) || '%'
     AND JT.ENABLED = 1
         AND ROWNUM = 1) as JobType,
                JA.INPUT_PARAMS as InputParams,
      JA.CREATED_AT as CreatedAt,
        JA.STARTED_AT as StartedAt,
                JA.COMPLETED_AT as CompletedAt,
       JA.STATUS as Status,
        JA.PROGRESS_PCT as ProgressPct,
                JA.RESULT_DATA as ResultData,
                JA.ERROR_MESSAGE as ErrorMessage,
        JA.DURATION_SECONDS as DurationSeconds,
      JA.CREATED_BY as CreatedBy,
         JA.MODIFIED_AT as ModifiedAt
   FROM SEGSOCIAL.JOB_AUDIT JA
 WHERE JA.JOB_ID = :JobId";

        return await unitOfWork.Connection.QueryFirstOrDefaultAsync<JobAuditDto>(
                sql,
         new { JobId = jobId });
    }

    /// <inheritdoc/>
    public async Task<List<JobAuditLogDto>> GetJobLogsAsync(string jobId)
    {
        const string sql = @"
                            SELECT 
                            LOG_ID as LogId,
                            LOG_TIMESTAMP as LogTimestamp,
                            LOG_LEVEL as LogLevel,
                            LOG_MESSAGE as LogMessage,
                            PROGRESS_PCT as ProgressPct
                            FROM SEGSOCIAL.JOB_AUDIT_LOGS
                            WHERE JOB_ID = :JobId
                            ORDER BY PROGRESS_PCT, LOG_TIMESTAMP";

        var result = await unitOfWork.Connection.QueryAsync<JobAuditLogDto>(
                                                                             sql,
                                                                             new { JobId = jobId });

        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<JobAuditsPaginadasDto> GetJobAuditsAsync(
                                                                string? createdBy = null,
                                                                DateTime? fechaInicio = null,
                                                                int? jobType = null,
                                                                string? jobId = null,
                                                                DateTime? periodo = null,
                                                                int page = 1,
                                                                int pageSize = 10)
    {
        try
        {
            var whereConditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(createdBy))
            {
                whereConditions.Add("UPPER(JA.CREATED_BY) LIKE UPPER(:CreatedBy)");
                parameters.Add("CreatedBy", $"%{createdBy}%", DbType.String);
            }

            if (fechaInicio.HasValue)
            {
                whereConditions.Add("TRUNC(JA.CREATED_AT) = TRUNC(:FechaInicio)");
                parameters.Add("FechaInicio", fechaInicio.Value, DbType.DateTime);
            }

            if (jobType.HasValue)
            {
                var sqlJobType = "SELECT ENDPOINT FROM SEGSOCIAL.JOB_TYPE WHERE ID = :JobType AND ENABLED = 1";
                var endpoint = await unitOfWork.Connection.QueryFirstOrDefaultAsync<string>(
                    sqlJobType,
                    new { JobType = jobType.Value });

                whereConditions.Add("UPPER(JA.ENDPOINT) LIKE UPPER(:Endpoint)");
                parameters.Add("Endpoint", $"%{endpoint}%", DbType.String);
            }

            if (!string.IsNullOrWhiteSpace(jobId))
            {
                whereConditions.Add("JA.JOB_ID = :JobId");
                parameters.Add("JobId", jobId, DbType.String);
            }

            if (periodo.HasValue)
            {
                // Oracle 11g compatible: Convertir CLOB a VARCHAR2 para búsqueda
                whereConditions.Add("DBMS_LOB.SUBSTR(JA.INPUT_PARAMS, 4000, 1) LIKE :Periodo");
                parameters.Add("Periodo", $"%{periodo.Value:yyyy-MM}%", DbType.String);
            }

            var whereClause = whereConditions.Count > 0
  ? "WHERE " + string.Join(" AND ", whereConditions)
  : string.Empty;

            // Obtener el conteo total de registros
            var countSql = $@"SELECT COUNT(*) 
  FROM SEGSOCIAL.JOB_AUDIT JA
         {whereClause}";
            var totalRegistros = await unitOfWork.Connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Obtener los registros paginados
            var sql = $@"
         SELECT JA.AUDIT_ID AS AuditId,
         JA.JOB_ID AS JobId,
         JA.ENDPOINT AS Endpoint,
         (SELECT JT.NAME
            FROM SEGSOCIAL.JOB_TYPE JT
           WHERE     UPPER (JA.ENDPOINT) LIKE '%' || UPPER (JT.ENDPOINT) || '%'
                 AND JT.ENABLED = 1
                 AND ROWNUM = 1)
            AS JobType,
         JA.INPUT_PARAMS AS InputParams,
         JA.CREATED_AT AS CreatedAt,
         JA.STARTED_AT AS StartedAt,
         JA.COMPLETED_AT AS CompletedAt,
         JA.STATUS AS Status,
         JA.PROGRESS_PCT AS ProgressPct,
         JA.RESULT_DATA AS ResultData,
         JA.ERROR_MESSAGE AS ErrorMessage,
         JA.DURATION_SECONDS AS DurationSeconds,
         SUBSTR(JA.CREATED_BY, 3) AS CreatedBy,
         JA.MODIFIED_AT AS ModifiedAt
    FROM SEGSOCIAL.JOB_AUDIT JA
    {whereClause}
    ORDER BY JA.CREATED_AT DESC";

            // Agregar paginación con ROWNUM
            parameters.Add("PageSize", pageSize, DbType.Int32);
            parameters.Add("Page", page, DbType.Int32);

            var paginatedSql = $@"
                                SELECT *
                                    FROM (SELECT a.*, ROWNUM rnum
                                    FROM ({sql}) a
                                WHERE ROWNUM <= :PageSize * :Page) b
                                WHERE rnum > :PageSize * (:Page - 1)";

            var audits = await unitOfWork.Connection.QueryAsync<JobAuditDto>(paginatedSql, parameters);

            return new JobAuditsPaginadasDto
            {
                TotalRegistros = totalRegistros,
                Audits = audits.ToList(),
            };
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener auditorías de jobs: {ex.Message}");
        }
    }
}
