namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System.Threading.Tasks;

using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("elasticsearch_index_query")]
[HealthTags("infra", "elastic", "query")]
public class ElasticIndexQueryHealthCheck : IHealthCheck
{
    private readonly IElasticIndexService _indexService;
    private readonly ILogger<ElasticIndexQueryHealthCheck> _logger;

    public ElasticIndexQueryHealthCheck(
        IElasticIndexService indexService,
        ILogger<ElasticIndexQueryHealthCheck> logger)
    {
        _indexService = indexService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var indices = await _indexService.GetIndexNamesAsync(cancellationToken);

        if (indices.Count > 0)
        {
            return HealthCheckResult.Healthy($"Elasticsearch has {indices.Count} indices.");
        }

        _logger.LogWarning("Elastic index listesi boş ya da erişilemedi.");
        return HealthCheckResult.Degraded("Elastic index listesi alınamadı veya boş.");
    }
}
