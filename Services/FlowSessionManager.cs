using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Oracle.ManagedDataAccess.Client;

namespace SeguridadSocialApi.Services
{
    public interface IFlowSessionManager
    {
        Task<string> StartAsync();
        OracleConnection? GetConnection(string flowId);
        Task EndAsync(string flowId);
    }

    internal sealed class FlowSession
    {
        public required OracleConnection Connection { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public class FlowSessionManager : IFlowSessionManager
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly MemoryCacheEntryOptions _cacheOptions;

        private const string CacheKeyPrefix = "flow-session:";

        public FlowSessionManager(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;

            var minutes = _configuration.GetValue<int?>("FlowSession:ExpirationMinutes") ?? 10;
            _cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(minutes),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes * 2)
            };
        }

        public async Task<string> StartAsync()
        {
            // Build a dedicated Oracle connection with pooling disabled to preserve a single session across requests
            var baseConnStr = _configuration["OracleConfig:ConnectionString"]
                ?? throw new InvalidOperationException("Oracle connection string not configured.");

            var builder = new OracleConnectionStringBuilder(baseConnStr)
            {
                Pooling = false
            };

            var conn = new OracleConnection(builder.ConnectionString);
            await conn.OpenAsync();

            var flowId = Guid.NewGuid().ToString("N");
            _cache.Set(CacheKey(flowId), new FlowSession { Connection = conn }, _cacheOptions);
            return flowId;
        }

        public OracleConnection? GetConnection(string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId)) return null;
            return _cache.TryGetValue(CacheKey(flowId), out FlowSession? session) ? session!.Connection : null;
        }

        public Task EndAsync(string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId)) return Task.CompletedTask;
            if (_cache.TryGetValue(CacheKey(flowId), out FlowSession? session))
            {
                _cache.Remove(CacheKey(flowId));
                try
                {
                    session!.Connection.Dispose();
                }
                catch { /* ignore */ }
            }
            return Task.CompletedTask;
        }

        private static string CacheKey(string flowId) => CacheKeyPrefix + flowId;
    }
}
