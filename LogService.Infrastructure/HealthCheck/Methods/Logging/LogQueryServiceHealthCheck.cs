namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("log_query_service_check")]
[HealthTags("elastic", "log-query", "role", "search", "ready")]
public class LogQueryServiceHealthCheck(ILogQueryService logQueryService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var filter = new LogFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow,
            Page = 1,
            PageSize = 1
        };

        var result = await logQueryService.QueryLogsFlexibleAsync(
            indexName: LogConstants.DataStreamName,
            role: "Admin",
            filter: filter,
            fetchCount: true,
            fetchDocuments: false,
            includeFields: null
        );

        return result.IsSuccess
            ? HealthCheckResult.Healthy("Log query service responded successfully.")
            : HealthCheckResult.Unhealthy($"Log query failed: {string.Join(", ", result.Errors)}");
    }
}
