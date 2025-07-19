namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("elastic_cluster")]
[HealthTags("elastic", "critical", "cluster")]
public class ElasticClusterHealthCheck : IHealthCheck
{
    private readonly IElasticHealthService _elasticHealth;
    private readonly IElasticIndexService _indexService;
    private readonly IElasticLogClient _logClient;

    public ElasticClusterHealthCheck(
        IElasticHealthService elasticHealth,
        IElasticIndexService indexService,
        IElasticLogClient logClient)
    {
        _elasticHealth = elasticHealth;
        _indexService = indexService;
        _logClient = logClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pingOk = await _elasticHealth.IsElasticAvailableAsync(cancellationToken);
        if (!pingOk)
            return HealthCheckResult.Unhealthy("Elasticsearch ping başarısız");

        var indices = await _indexService.GetIndexNamesAsync(cancellationToken);
        if (indices.Count == 0)
            return HealthCheckResult.Degraded("Elasticsearch index listesi boş");

        var dummyFilter = new LogFilterDto
        {
            Page = 1,
            PageSize = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow
        };

        var result = await _logClient.QueryLogsFlexibleAsync(
            indexName: indices[0],
            filter: dummyFilter,
            allowedLevels: [LogSeverityCode.Information],
            fetchDocuments: false,
            fetchCount: false,
            includeFields: []
        );

        return result.IsSuccess
            ? HealthCheckResult.Healthy("Elastic sorgu başarılı")
            : HealthCheckResult.Degraded($"Elastic sorgu başarısız: {string.Join(" | ", result.Errors)}");
    }
}
