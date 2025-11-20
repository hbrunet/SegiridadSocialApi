using Oracle.ManagedDataAccess.Client;

namespace SeguridadSocialApi.Services.Interfaces
{
    public interface IFlowSessionManager
    {
        Task<string> StartAsync();
        OracleConnection? GetConnection(string flowId);
        Task EndAsync(string flowId);
    }
}
