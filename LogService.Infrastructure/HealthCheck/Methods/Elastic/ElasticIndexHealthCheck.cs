using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;

[Name("elastic_index_check")]
[HealthTags("elastic", "index", "read", "ready")]
public class ElasticIndexHealthCheck(IElasticIndexService indexService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var indices = await indexService.GetIndexNamesAsync(cancellationToken);

            if (indices is null || indices.Count == 0)
            {
                return HealthCheckResult.Degraded("Elasticsearch reachable, but no indices found.");
            }

            return HealthCheckResult.Healthy($"Found {indices.Count} indices.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Elasticsearch index listing failed.", ex);
        }
    }
}
