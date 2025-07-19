namespace LogService.Infrastructure.HealthCheck.Methods.Redis;
using System;
using System.Threading.Tasks;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("string_distributed_cache")]
[HealthTags("redis", "cache", "string")]
public class StringDistributedCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<StringDistributedCacheHealthCheck> _logger;

    public StringDistributedCacheHealthCheck(
        IDistributedCache cache,
        ILogger<StringDistributedCacheHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        const string key = "healthcheck:string-cache:test";
        const string value = "ok";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
        };

        try
        {
            await _cache.SetStringAsync(key, value, options, cancellationToken);
            var retrieved = await _cache.GetStringAsync(key, cancellationToken);

            if (retrieved != value)
            {
                return HealthCheckResult.Degraded("Cache string değeri okunamadı.");
            }

            return HealthCheckResult.Healthy("String cache okuma/yazma başarılı.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StringDistributedCache health check hatası.");
            return HealthCheckResult.Unhealthy("String cache servisi erişilemiyor.", ex);
        }
    }
}
