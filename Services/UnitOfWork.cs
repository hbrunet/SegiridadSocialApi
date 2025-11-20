using System.Data;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IOracleConnectionFactory _connectionFactory;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;
        private bool _ownsConnection = true;

        public UnitOfWork(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = _connectionFactory.CreateConnection();
                }

                // Abrir la conexión si no está abierta para mantener la misma sesión
                // Esto es crítico para tablas temporales de Oracle (datos por sesión)
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                return _connection;
            }
        }

        public IDbTransaction? Transaction => _transaction;

        public void BeginTransaction()
        {
            // La conexión ya está abierta por el getter de Connection
            _transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No hay transacción activa para hacer commit.");

            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No hay transacción activa para hacer rollback.");

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    if (_ownsConnection)
                    {
                        _connection?.Close();
                        _connection?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        // Permite inyectar una conexión externa (por ejemplo, una sesión fijada para flujos de dos pasos)
        public void UseExternalConnection(IDbConnection externalConnection, bool ownsConnection = false)
        {
            _connection = externalConnection ?? throw new ArgumentNullException(nameof(externalConnection));
            _ownsConnection = ownsConnection;
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }
    }
}

