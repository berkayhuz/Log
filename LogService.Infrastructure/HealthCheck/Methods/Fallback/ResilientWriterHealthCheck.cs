namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Threading.Tasks;

using LogService.Application.Resilience;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("resilient_writer_check")]
[HealthTags("resilience", "retry", "circuit-breaker", "fallback", "elastic", "write")]
public class ResilientWriterHealthCheck(IResilientLogWriter writer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var log = new LogEntryDto
        {
            Timestamp = DateTime.UtcNow,
            Message = "HealthCheck - Resilient Writer Test Log",
            Level = LogStage.Information
        };

        try
        {
            var result = await writer.WriteWithRetryAsync(log, cancellationToken);

            if (result.IsSuccess)
                return HealthCheckResult.Healthy("Resilient writer succeeded.");

            return HealthCheckResult.Degraded($"Resilient writer failed. Fallback likely used. Errors: {string.Join(", ", result.Errors)}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Resilient writer threw exception.", ex);
        }
    }
}
