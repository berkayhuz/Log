namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

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
            StartDate = DateTime.UtcNow.AddMinutes(-15),
            EndDate = DateTime.UtcNow,
            Page = 1,
            PageSize = 1
        };

        var result = await _logClient.QueryLogsFlexibleAsync(
            indexName: "logservice-logs-*",
            filter: filter,
            allowedLevels: new() { LogSeverityCode.Information, LogSeverityCode.Warning, LogSeverityCode.Error },
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
            return HealthCheckResult.Degraded("Log sorgusu başarılı ancak son 15 dakikada kayıt bulunamadı.");
        }

        return HealthCheckResult.Healthy($"Son 15 dakikada {result.Value.TotalCount} kayıt bulundu.");
    }
}
