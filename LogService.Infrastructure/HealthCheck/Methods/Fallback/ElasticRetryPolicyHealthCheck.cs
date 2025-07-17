namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Fallback;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("elastic_retry_policy_check")]
[HealthTags("elastic", "retry", "polly", "fallback", "write")]
public class ElasticRetryPolicyHealthCheck(ILogEntryWriteService writer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
    {
        var testLog = new LogEntryDto
        {
            Level = LogStage.Information,
            Message = "Elastic write test log",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            var result = await writer.WriteToElasticAsync(testLog);

            return result.IsSuccess
                ? HealthCheckResult.Healthy("Elastic write succeeded.")
                : HealthCheckResult.Unhealthy("Elastic write failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Elastic write threw exception.", ex);
        }
    }

}
