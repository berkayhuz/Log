namespace LogService.Infrastructure.HealthCheck.Methods.Redis;

using System.Text.Json;

using LogService.Application.Abstractions.Caching;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
[Name("redis_cache")]
[Tags("cache", "redis", "string")]
public sealed class RedisCacheHealthCheck(ICacheService cacheService)
    : IHealthCheck
{
    private const string TestValue = "HealthCheckPing";
    private static readonly TimeSpan TestExpiration = TimeSpan.FromSeconds(10);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var testKey = $"healthcheck:redis:{Guid.NewGuid()}";

        try
        {
            await cacheService.SetAsync(testKey, TestValue, TestExpiration);
            var retrieved = await cacheService.GetAsync<string>(testKey);

            if (retrieved != TestValue)
            {
                return HealthCheckResult.Unhealthy("Redis cache returned incorrect value.");
            }

            return HealthCheckResult.Healthy("Redis cache operational.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis cache threw exception.", ex);
        }
    }
}
