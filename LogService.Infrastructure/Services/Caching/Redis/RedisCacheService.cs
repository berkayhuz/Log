namespace LogService.Infrastructure.Services.Caching.Redis;

using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        try
        {
            var bytes = await _cache.GetAsync(key);
            var json = bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
            return string.IsNullOrEmpty(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, _serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache’den okunurken hata: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(value, _serializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            await _cache.SetAsync(key, System.Text.Encoding.UTF8.GetBytes(json), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache’e yazılırken hata: {Key}", key);
        }
    }
}
