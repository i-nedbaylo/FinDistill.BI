using FinDistill.Domain.Interfaces;

namespace FinDistill.Infrastructure.Caching;

/// <summary>
/// No-op implementation of ICacheService.
/// Used when Redis is disabled (Features:UseRedis = false).
/// Always returns null from GetAsync, making cache-aside logic transparent.
/// </summary>
public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct)
        => Task.CompletedTask;
}
