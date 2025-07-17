namespace LogService.Infrastructure.HealthCheck.Methods.Message;
using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Messages;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("redis_log_message_provider_check")]
[HealthTags("redis", "log-messages", "cache", "lookup", "ready")]
public class RedisLogMessageProviderHealthCheck(ILogMessageProvider messageProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = messageProvider.GetKeys();
            if (!keys.Any())
            {
                return Task.FromResult(HealthCheckResult.Degraded("No log message keys found in Redis."));
            }

            var firstKey = keys.FirstOrDefault();
            var msg = messageProvider.Get(firstKey ?? "UNKNOWN_KEY");

            return string.IsNullOrWhiteSpace(msg)
                ? Task.FromResult(HealthCheckResult.Unhealthy("Message value not found for first Redis key."))
                : Task.FromResult(HealthCheckResult.Healthy("Log message provider returned a value successfully."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Redis log message provider failed.", ex));
        }
    }
}
