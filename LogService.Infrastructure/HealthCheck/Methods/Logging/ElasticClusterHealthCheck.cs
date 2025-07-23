namespace LogService.Infrastructure.HealthCheck.Methods.Logging;

using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using SharedKernel.Common.Results.Objects;

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
        var pingResult = await _elasticHealth.IsElasticAvailableAsync(cancellationToken);
        if (pingResult.IsFailure || !pingResult.Value)
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
            allowedLevels: [ErrorLevel.Information],
            fetchDocuments: false,
            fetchCount: false,
            includeFields: []
        );

        return result.IsSuccess
            ? HealthCheckResult.Healthy("Elastic sorgu başarılı")
            : HealthCheckResult.Degraded($"Elastic sorgu başarısız: {string.Join(" | ", result.Errors)}");
    }
}
