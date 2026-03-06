namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Abstraction for caching service.
/// Default implementation is a no-op (NullCacheService).
/// Can be swapped to Redis via Features:UseRedis configuration.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct) where T : class;

    Task RemoveAsync(string key, CancellationToken ct);
}
