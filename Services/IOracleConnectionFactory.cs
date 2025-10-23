using System.Data;

namespace SeguridadSocialApi.Services
{
    public interface IOracleConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
