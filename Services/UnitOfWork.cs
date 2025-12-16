// <copyright file="UnitOfWork.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Data;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly IOracleConnectionFactory connectionFactory;
    private IDbConnection? connection;
    private IDbTransaction? transaction;
    private bool disposed;
    private bool ownsConnection = true;

    public UnitOfWork(IOracleConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    /// <inheritdoc/>
    public IDbConnection Connection
    {
        get
        {
            if (connection == null)
            {
                connection = connectionFactory.CreateConnection();
            }

            // Abrir la conexión si no está abierta para mantener la misma sesión
            // Esto es crítico para tablas temporales de Oracle (datos por sesión)
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }
    }

    /// <inheritdoc/>
    public IDbTransaction? Transaction => transaction;

    /// <inheritdoc/>
    public void BeginTransaction()
    {
        // La conexión ya está abierta por el getter de Connection
        transaction = Connection.BeginTransaction();
    }

    /// <inheritdoc/>
    public void Commit()
    {
        if (transaction == null)
        {
            throw new InvalidOperationException("No hay transacción activa para hacer commit.");
        }

        try
        {
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            transaction.Dispose();
            transaction = null;
        }
    }

    /// <inheritdoc/>
    public void Rollback()
    {
        if (transaction == null)
        {
            throw new InvalidOperationException("No hay transacción activa para hacer rollback.");
        }

        transaction.Rollback();
        transaction.Dispose();
        transaction = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                transaction?.Dispose();
                if (ownsConnection)
                {
                    connection?.Close();
                    connection?.Dispose();
                }
            }

            disposed = true;
        }
    }

    // Permite inyectar una conexión externa (por ejemplo, una sesión fijada para flujos de dos pasos)

    /// <inheritdoc/>
    public void UseExternalConnection(IDbConnection externalConnection, bool ownsConnection = false)
    {
        connection = externalConnection ?? throw new ArgumentNullException(nameof(externalConnection));
        this.ownsConnection = ownsConnection;
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }
}
