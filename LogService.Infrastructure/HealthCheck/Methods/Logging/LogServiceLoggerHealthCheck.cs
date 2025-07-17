namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("log_service_logger_check")]
[HealthTags("logging", "internal", "resilient", "fallback", "ready")]
public class LogServiceLoggerHealthCheck(ILogServiceLogger logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await logger.LogAsync(
                LogStage.Information,
                "HealthCheck: LogServiceLogger test log.");

            return HealthCheckResult.Healthy("LogServiceLogger log entry sent successfully.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("LogServiceLogger failed to log internally.", ex);
        }
    }
}
