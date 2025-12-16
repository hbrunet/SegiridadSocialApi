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

    public JobAuditRepository(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<long> InsertJobAuditAsync(
                                        string jobId,
                                        string jobName,
                                        string jobType,
                                        string inputParamsJson,
                                        string? usuario = null,
                                        string? ipAddress = null,
                                        string? userAgent = null)
    {
        // Para Oracle con parámetro de salida, necesitamos una solución híbrida
        const string sql = @"
BEGIN
        SEGSOCIAL.JOBS_MONITOR.JOB_AUDIT_INSERT(
     :p_job_id,
:p_job_name,
       :p_job_type,
      :p_input_params,
   :p_usuario,
    :p_ip_address,
     :p_user_agent,
  :p_audit_id
    );
  END;";

        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);
        parameters.Add("p_job_name", jobName, DbType.String, ParameterDirection.Input);
        parameters.Add("p_job_type", jobType, DbType.String, ParameterDirection.Input);
        parameters.Add("p_input_params", inputParamsJson, DbType.String, ParameterDirection.Input);
        parameters.Add("p_usuario", usuario, DbType.String, ParameterDirection.Input);
        parameters.Add("p_ip_address", ipAddress, DbType.String, ParameterDirection.Input);
        parameters.Add("p_user_agent", userAgent, DbType.String, ParameterDirection.Input);
        parameters.Add("p_audit_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await unitOfWork.Connection.ExecuteAsync(sql, parameters);

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
                        string? errorMessage = null,
                        int? registrosProcesados = null,
                        int? registrosErrores = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_job_id", jobId, DbType.String, ParameterDirection.Input);
        parameters.Add("p_status", status, DbType.String, ParameterDirection.Input);
        parameters.Add("p_progress_pct", progressPct, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_result_data", resultDataJson, DbType.String, ParameterDirection.Input);
        parameters.Add("p_error_message", errorMessage, DbType.String, ParameterDirection.Input);
        parameters.Add("p_registros_procesados", registrosProcesados, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_registros_errores", registrosErrores, DbType.Int32, ParameterDirection.Input);

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
                                AUDIT_ID as AuditId,
                                JOB_ID as JobId,
                                INTERNAL_JOB_ID as InternalJobId,
                                JOB_NAME as JobName,
                                JOB_TYPE as JobType,
                                INPUT_PARAMS as InputParams,
                                CREATED_AT as CreatedAt,
                                STARTED_AT as StartedAt,
                                COMPLETED_AT as CompletedAt,
                                STATUS as Status,
                                PROGRESS_PCT as ProgressPct,
                                RESULT_DATA as ResultData,
                                ERROR_MESSAGE as ErrorMessage,
                                DURATION_SECONDS as DurationSeconds,
                                REGISTROS_PROCESADOS as RegistrosProcesados,
                                REGISTROS_ERRORES as RegistrosErrores,
                                USUARIO as Usuario,
                                IP_ADDRESS as IpAddress,
                                USER_AGENT as UserAgent
                                FROM SEGSOCIAL.JOB_AUDIT
                                WHERE JOB_ID = :JobId";

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
                                ORDER BY LOG_TIMESTAMP";

        var result = await unitOfWork.Connection.QueryAsync<JobAuditLogDto>(
                   sql,
                   new { JobId = jobId });

        return result.ToList();
    }
}
