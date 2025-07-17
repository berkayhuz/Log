namespace LogService.Infrastructure.HealthCheck.Methods.Redis;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("string_distributed_cache")]
[HealthTags("cache", "redis", "string")]
public class StringDistributedCacheHealthCheck(IStringDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = $"healthcheck:string:{Guid.NewGuid()}";
            var testValue = "pong";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            };

            await cache.SetStringAsync(testKey, testValue, options, cancellationToken);
            var retrieved = await cache.GetStringAsync(testKey, cancellationToken);

            if (retrieved == testValue)
            {
                return HealthCheckResult.Healthy("StringDistributedCache operational.");
            }

            return HealthCheckResult.Unhealthy("Failed to verify string cache read/write.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception during string cache check.", ex);
        }
    }
}
