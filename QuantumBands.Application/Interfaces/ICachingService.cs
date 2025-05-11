// QuantumBands.Application/Interfaces/ICachingService.cs
namespace QuantumBands.Application.Interfaces;

public interface ICachingService
{
    /// <summary>
    /// Gets a value from the cache by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The cached value, or default(T) if the key is not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="absoluteExpirationRelativeToNow">Optional absolute expiration time.</param>
    /// <param name="slidingExpiration">Optional sliding expiration time.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache. If not found, executes the factory function, caches its result, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to generate the value if not found in cache.</param>
    /// <param name="absoluteExpirationRelativeToNow">Optional absolute expiration time for new cache entry.</param>
    /// <param name="slidingExpiration">Optional sliding expiration time for new cache entry.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The cached or newly generated value.</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default);
}