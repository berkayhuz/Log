namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System;
using System.Threading.Tasks;

using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Objects;

[Name("elasticsearch_log_query")]
[HealthTags("elastic", "infra", "log", "query")]
public class ElasticLogQueryHealthCheck : IHealthCheck
{
    private readonly IElasticLogClient _logClient;
    private readonly ILogger<ElasticLogQueryHealthCheck> _logger;

    public ElasticLogQueryHealthCheck(
        IElasticLogClient logClient,
        ILogger<ElasticLogQueryHealthCheck> logger)
    {
        _logClient = logClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var filter = new LogFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow,
            Page = 1,
            PageSize = 1
        };

        var result = await _logClient.QueryLogsFlexibleAsync(
            indexName: "logservice-logs-*",
            filter: filter,
            allowedLevels: new() { ErrorLevel.Debug, ErrorLevel.Information, ErrorLevel.Warning, ErrorLevel.Error },
            fetchCount: true,
            fetchDocuments: false
        );

        if (result.IsFailure)
        {
            _logger.LogWarning("Elastic log sorgusu başarısız: {Errors}", string.Join(" | ", result.Errors));
            return HealthCheckResult.Unhealthy("Log sorgusu başarısız: " + string.Join(" | ", result.Errors));
        }

        if (result.Value.TotalCount == 0)
        {
            return HealthCheckResult.Degraded("Log sorgusu başarılı ancak son 2 günde kayıt bulunamadı.");
        }

        return HealthCheckResult.Healthy($"Son 2 günde {result.Value.TotalCount} kayıt bulundu.");
    }
}
