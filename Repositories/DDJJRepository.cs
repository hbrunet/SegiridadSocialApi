// <copyright file="DDJJRepository.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using System.Text;
using Dapper;
using Oracle.ManagedDataAccess.Client;
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
    public async Task FusionarDatosAsync(IDbConnection connection, DateTime periodo, long? cuil = null, string? jobId = null)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("vPeriodo", periodo, DbType.DateTime, ParameterDirection.Input);
            parameters.Add("vCuil", cuil, DbType.Int64, ParameterDirection.Input);
            parameters.Add("vJobId", jobId, DbType.String, ParameterDirection.Input);

            await connection.ExecuteAsync(
                "SEGSOCIAL.PKG_DDJJ_UNIFICADOR_931.ORQUESTAR_DDJJ_UNIFICACION",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al procesar unificación del periodo {periodo:yyyy-MM} para CUIL {cuil}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ExportarPresentacionAsync(IDbConnection connection, DateTime periodo, Stream outputStream, CancellationToken cancellationToken = default)
    {
        try
        {
            // La vista retorna cada registro como una línea de texto ya formateada
            const string sql = """
                SELECT CADENA
                FROM SEGSOCIAL.VW_PRESENTACION_DDJJ
                WHERE PERIODO = :periodo
                ORDER BY CUIL
                """;

            var oracleConn = (OracleConnection)connection;
            await using var cmd = new OracleCommand(sql, oracleConn);
            cmd.Parameters.Add(new OracleParameter("periodo", OracleDbType.Date) { Value = periodo });

            // RowSize es 0 antes de ejecutar, por lo que no se puede usar para calcular FetchSize.
            // Se fija un buffer de 4 MB para minimizar round-trips con filas de texto grandes.
            cmd.FetchSize = 4 * 1024 * 1024;

            await using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
            int count = 0;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var linea = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                await writer.WriteLineAsync(linea.AsMemory(), cancellationToken);
                count++;

                // Flush cada 1000 líneas para no acumular todo en buffer
                if (count % 1000 == 0)
                {
                    await writer.FlushAsync(cancellationToken);
                }
            }

            await writer.FlushAsync(cancellationToken);
            return count;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al exportar presentación del periodo {periodo:yyyy-MM}: {ex.Message}", ex);
        }
    }
}
