using System.Data;

namespace SeguridadSocialApi.Services.Interfaces
{
    public interface IOracleConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
