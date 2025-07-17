namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Fallback;
using LogService.Application.Resilience;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("fallback_reprocessing_check")]
[HealthTags("fallback", "retry", "resilient", "elastic", "write")]
public class FallbackReprocessingHealthCheck(
    IFallbackLogWriter fallbackWriter,
    IElasticHealthService elasticHealth,
    IResilientLogWriter resilientWriter) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var fallbackDto = new LogEntryDto
        {
            Timestamp = DateTime.UtcNow,
            Message = "HealthCheck Fallback Test Log",
            Level = LogStage.Information,
            Source = "FallbackReprocessingHealthCheck"
        };

        var elasticUp = await elasticHealth.IsElasticAvailableAsync(cancellationToken);

        if (elasticUp)
        {
            return HealthCheckResult.Healthy("Elastic is available, fallback queue not active.");
        }

        var result = await resilientWriter.WriteWithRetryAsync(fallbackDto, cancellationToken);

        return result.IsSuccess
            ? HealthCheckResult.Healthy("Fallback write succeeded while Elastic was unavailable.")
            : HealthCheckResult.Unhealthy("Fallback write failed.");
    }
}
