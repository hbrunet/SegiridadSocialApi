// <copyright file="FlowSessionManager.cs" company="Seguridad Social API">
// Copyright (c) Seguridad Social API. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Oracle.ManagedDataAccess.Client;
using SeguridadSocialApi.Services.Interfaces;

namespace SeguridadSocialApi.Services;

internal sealed class FlowSession
{
    public required OracleConnection Connection { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public class FlowSessionManager : IFlowSessionManager
{
    private readonly IMemoryCache cache;
    private readonly IConfiguration configuration;
    private readonly MemoryCacheEntryOptions cacheOptions;

    private const string CacheKeyPrefix = "flow-session:";

    public FlowSessionManager(IMemoryCache cache, IConfiguration configuration)
    {
        this.cache = cache;
        this.configuration = configuration;

        var minutes = this.configuration.GetValue<int?>("FlowSession:ExpirationMinutes") ?? 10;
        cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(minutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes * 2),
        };
    }

    /// <inheritdoc/>
    public async Task<string> StartAsync()
    {
        // Build a dedicated Oracle connection with pooling disabled to preserve a single session across requests
        var baseConnStr = configuration["OracleConfig:ConnectionString"]
            ?? throw new InvalidOperationException("Oracle connection string not configured.");

        var builder = new OracleConnectionStringBuilder(baseConnStr)
        {
            Pooling = false,
        };

        var conn = new OracleConnection(builder.ConnectionString);
        await conn.OpenAsync();

        var flowId = Guid.NewGuid().ToString("N");
        cache.Set(CacheKey(flowId), new FlowSession { Connection = conn }, cacheOptions);
        return flowId;
    }

    /// <inheritdoc/>
    public OracleConnection? GetConnection(string flowId)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return null;
        }

        return cache.TryGetValue(CacheKey(flowId), out FlowSession? session) ? session!.Connection : null;
    }

    /// <inheritdoc/>
    public Task EndAsync(string flowId)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return Task.CompletedTask;
        }

        if (cache.TryGetValue(CacheKey(flowId), out FlowSession? session))
        {
            cache.Remove(CacheKey(flowId));
            try
            {
                session!.Connection.Dispose();
            }
            catch
            { /* ignore */
            }
        }

        return Task.CompletedTask;
    }

    private static string CacheKey(string flowId) => CacheKeyPrefix + flowId;
}
