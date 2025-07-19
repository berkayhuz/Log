namespace LogService.Infrastructure.HealthCheck.Methods.Redis;
using System;
using System.Threading.Tasks;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

[Name("redis_region_support")]
[HealthTags("redis", "cache", "region")]
public class RedisCacheRegionSupportHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheRegionSupportHealthCheck> _logger;

    public RedisCacheRegionSupportHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheRegionSupportHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var testKey = "healthcheck:redis:region:test";
            var testValue = Guid.NewGuid().ToString();

            await db.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            var value = await db.StringGetAsync(testKey);

            if (value != testValue)
            {
                return HealthCheckResult.Degraded("Redis yazma/okuma testi başarısız.");
            }

            return HealthCheckResult.Healthy("Redis region cache okuma/yazma çalışıyor.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis region destek sağlık kontrolü sırasında hata oluştu.");
            return HealthCheckResult.Unhealthy("Redis region desteği başarısız.", ex);
        }
    }
}
