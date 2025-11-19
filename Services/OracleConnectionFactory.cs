using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Services.Interfaces;
using System.Data;

namespace SeguridadSocialApi.Services
{
    public class OracleConnectionFactory : IOracleConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration["OracleConfig:ConnectionString"]
                ?? throw new InvalidOperationException("Oracle connection string not configured.");
        }

        public IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}

