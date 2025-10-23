using System.Data;

namespace SeguridadSocialApi.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
        void BeginTransaction();
        void Commit();
        void Rollback();
        void UseExternalConnection(IDbConnection externalConnection, bool ownsConnection = false);
    }
}
