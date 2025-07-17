namespace LogService.Infrastructure.Services.Caching;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Requests;

using Microsoft.Extensions.Caching.Distributed;

public class RedisCacheService(IStringDistributedCache cache)
    : ICacheService, ICacheRegionSupport
{
    private readonly IStringDistributedCache _cache = cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        var cachedData = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(cachedData))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        };

        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options);
    }

    private string GetRegionKey(string region) => $"cache:region:{region}";

    public async Task AddKeyToRegionAsync(string region, string key)
    {
        try
        {
            var regionKey = GetRegionKey(region);
            var json = await _cache.GetStringAsync(regionKey);
            var keys = string.IsNullOrEmpty(json)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();

            keys.Add(key);
            await _cache.SetStringAsync(regionKey, JsonSerializer.Serialize(keys));
        }
        catch (Exception)
        {
        }
    }

    public async Task InvalidateRegionAsync(string region)
    {
        try
        {
            var regionKey = GetRegionKey(region);
            var json = await _cache.GetStringAsync(regionKey);

            if (string.IsNullOrEmpty(json)) return;

            var keys = JsonSerializer.Deserialize<HashSet<string>>(json);
            if (keys == null || !keys.Any()) return;

            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }

            await _cache.RemoveAsync(regionKey);
        }
        catch (Exception)
        {
        }
    }
}
