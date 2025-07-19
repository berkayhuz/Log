namespace LogService.Infrastructure.HealthCheck.Methods.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("redis_cache_service")]
[HealthTags("redis", "cache", "distributed")]
public class RedisCacheServiceHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheServiceHealthCheck> _logger;

    public RedisCacheServiceHealthCheck(
        IDistributedCache cache,
        ILogger<RedisCacheServiceHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var testKey = "healthcheck:redis:distributed:test";
        var testValue = Guid.NewGuid().ToString();
        var duration = TimeSpan.FromSeconds(10);

        try
        {
            var json = JsonSerializer.Serialize(testValue);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            await _cache.SetStringAsync(testKey, json, options, cancellationToken);
            var value = await _cache.GetStringAsync(testKey, cancellationToken);

            if (value is null || JsonSerializer.Deserialize<string>(value) != testValue)
            {
                return HealthCheckResult.Degraded("Cache yazıldı ama geri okunamadı.");
            }

            return HealthCheckResult.Healthy("Cache okuma/yazma başarılı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedisCacheService health check hatası.");
            return HealthCheckResult.Unhealthy("Redis distributed cache erişilemiyor.", ex);
        }
    }
}
