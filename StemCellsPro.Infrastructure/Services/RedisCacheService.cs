using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StemCellsPro.Application.Interfaces;

namespace StemCellsPro.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var cachedResponse = await _cache.GetStringAsync(key);
        return cachedResponse == null ? default : JsonSerializer.Deserialize<T>(cachedResponse);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        var response = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromHours(1)
        };
        await _cache.SetStringAsync(key, response, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
