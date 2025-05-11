// QuantumBands.Infrastructure/Caching/InMemoryCachingService.cs
using Microsoft.Extensions.Caching.Memory;
using QuantumBands.Application.Interfaces; // Namespace của ICachingService
using Microsoft.Extensions.Logging;

namespace QuantumBands.Infrastructure.Caching;

public class InMemoryCachingService : ICachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCachingService> _logger;
    // Mặc định thời gian cache nếu không được cung cấp
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(15);


    public InMemoryCachingService(IMemoryCache memoryCache, ILogger<InMemoryCachingService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return Task.FromResult(value);
        }
        _logger.LogDebug("Cache miss for key: {CacheKey}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = new MemoryCacheEntryOptions();

        if (absoluteExpirationRelativeToNow.HasValue)
        {
            options.SetAbsoluteExpiration(absoluteExpirationRelativeToNow.Value);
        }
        else if (slidingExpiration.HasValue)
        {
            options.SetSlidingExpiration(slidingExpiration.Value);
        }
        else
        {
            // Áp dụng một chính sách mặc định nếu không có gì được chỉ định
            options.SetAbsoluteExpiration(DefaultAbsoluteExpiration);
        }

        _memoryCache.Set(key, value, options);
        _logger.LogDebug("Value cached for key: {CacheKey}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryCache.Remove(key);
        _logger.LogDebug("Cache removed for key: {CacheKey}", key);
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey} in GetOrCreateAsync", key);
            return value;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey} in GetOrCreateAsync. Fetching from factory.", key);
        T newValue = await factory();

        if (newValue != null) // Chỉ cache nếu giá trị không null
        {
            await SetAsync(key, newValue, absoluteExpirationRelativeToNow, slidingExpiration, cancellationToken);
        }
        return newValue;
    }
}