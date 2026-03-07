namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Abstraction for caching service.
/// Default implementation is a no-op (NullCacheService).
/// Can be swapped to Redis via Features:UseRedis configuration.
/// </summary>
public interface ICacheService
{
    /// <summary>Retrieves a cached value by key, or null if not found.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;

    /// <summary>Stores a value in cache with the specified expiration.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct) where T : class;

    /// <summary>Removes a cached entry by key.</summary>
    Task RemoveAsync(string key, CancellationToken ct);
}
