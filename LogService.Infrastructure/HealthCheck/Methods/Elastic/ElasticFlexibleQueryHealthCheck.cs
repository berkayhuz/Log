namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("elastic_flexible_query_check")]
[HealthTags("elastic", "query", "search", "ready")]
public class ElasticFlexibleQueryHealthCheck(IElasticLogClient logClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new LogFilterDto
            {
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow,
                Page = 1,
                PageSize = 1
            };

            var allowedLevels = new List<LogStage> { LogStage.Information, LogStage.Warning, LogStage.Error };

            var result = await logClient.QueryLogsFlexibleAsync(
                indexName: LogConstants.DataStreamName,
                filter: filter,
                allowedLevels: allowedLevels,
                fetchCount: true,
                fetchDocuments: false,
                includeFields: null
            );

            return result.IsSuccess
                ? HealthCheckResult.Healthy("Elastic flexible log query succeeded.")
                : HealthCheckResult.Unhealthy($"Elastic flexible log query failed: {result.IsFailure}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception during flexible log query.", ex);
        }
    }
}
