// <copyright file="OracleConnectionFactory.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Services;

public class OracleConnectionFactory : IOracleConnectionFactory
{
    private readonly string connectionString;

    public OracleConnectionFactory(IConfiguration configuration)
    {
        connectionString = configuration["OracleConfig:ConnectionString"]
            ?? throw new InvalidOperationException("Oracle connection string not configured.");
    }

    /// <inheritdoc/>
    public IDbConnection CreateConnection()
    {
        return new OracleConnection(connectionString);
    }
}
