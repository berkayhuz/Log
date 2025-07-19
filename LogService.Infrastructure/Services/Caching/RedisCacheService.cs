namespace LogService.Infrastructure.Services.Caching;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return await TryCatch.ExecuteAsync<T?>(
            tryFunc: async () =>
            {
                var data = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(data))
                    return default;

                return JsonSerializer.Deserialize<T>(data);
            },
            catchFunc: ex =>
            {
                _logger.LogError(ex, "Cache’den okunurken hata: {Key}", key);
                return Task.FromResult<T?>(default);
            },
            logger: _logger,
            context: $"RedisCacheService.GetAsync<{typeof(T).Name}>({key})"
        );
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration)
    {
        await TryCatch.ExecuteAsync(
            tryFunc: async () =>
            {
                var json = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                };
                await _cache.SetStringAsync(key, json, options);
            },
            logger: _logger,
            context: $"RedisCacheService.SetAsync<{typeof(T).Name}>({key})"
        );
    }
}
