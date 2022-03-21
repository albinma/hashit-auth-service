#nullable disable

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Internal;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

/// <summary>
/// Implementation of the ICache interface that uses the IDistributedCache service.
/// </summary>
/// <typeparam name="T"></typeparam>
public class DistributedCache<T> : ICache<T>
    where T : class
{
    private readonly IDistributedCache _cache;
    private readonly IdentityServerOptions _options;
    private readonly IConcurrencyLock<DistributedCache<T>> _concurrencyLock;
    private readonly ILogger<DefaultCache<T>> _logger;
    private const string KeySeparator = "-";

    public DistributedCache(
        IDistributedCache cache,
        IdentityServerOptions options,
        IConcurrencyLock<DistributedCache<T>> concurrencyLock,
        ILogger<DefaultCache<T>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _concurrencyLock = concurrencyLock ?? throw new ArgumentNullException(nameof(concurrencyLock));
        _logger = logger;
    }

    /// <summary>
    /// Used to create the key for the cache based on the data type being cached.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected string GetKey(string key)
    {
        return typeof(T).FullName + KeySeparator + key;
    }

    /// <summary>
    /// Gets the value from the cache.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<T> GetAsync(string key)
    {
        var formattedKey = GetKey(key);
        var cacheItemBytes = await _cache.GetAsync(formattedKey);

        if (cacheItemBytes == null)
            return null;

        var cacheItemString = Encoding.UTF8.GetString(cacheItemBytes);
        var cacheItem = JsonConvert.DeserializeObject<T>(cacheItemString);

        return cacheItem;
    }

    public async Task<T> GetOrAddAsync(string key, TimeSpan duration, Func<Task<T>> get)
    {
        var item = await GetAsync(key);

        if (item == null)
        {
            if (false == await _concurrencyLock.LockAsync((int)_options.Caching.CacheLockTimeout.TotalMilliseconds))
            {
                throw new Exception($"Failed to obtain cache lock for: '{GetType()}'");
            }

            try
            {
                // double check
                item = await GetAsync(key);

                if (item == null)
                {
                    _logger?.LogTrace("Cache miss for {cacheKey}", key);

                    item = await get();

                    if (item != null)
                    {
                        _logger?.LogTrace("Setting item in cache for {cacheKey}", key);
                        await SetAsync(key, item, duration);
                    }
                }
                else
                {
                    _logger?.LogTrace("Cache hit for {cacheKey}", key);
                }
            }
            finally
            {
                _concurrencyLock.Unlock();
            }
        }
        else
        {
            _logger?.LogTrace("Cache hit for {cacheKey}", key);
        }

        return item;
    }

    public async Task RemoveAsync(string key)
    {
        var formattedKey = GetKey(key);

        await _cache.RemoveAsync(formattedKey);
    }

    public async Task SetAsync(string key, T item, TimeSpan expiration)
    {
        var formattedKey = GetKey(key);
        var cacheItemString = JsonConvert.SerializeObject(item);
        var cacheItemBytes = Encoding.UTF8.GetBytes(cacheItemString);

        await _cache.SetAsync(formattedKey, cacheItemBytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }
}
