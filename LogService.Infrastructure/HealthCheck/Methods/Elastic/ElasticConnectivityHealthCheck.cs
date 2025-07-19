namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("elasticsearch_connectivity")]
[HealthTags("infra", "elastic")]
public class ElasticConnectivityHealthCheck : IHealthCheck
{
    private readonly IElasticHealthService _elasticHealthService;
    private readonly ILogger<ElasticConnectivityHealthCheck> _logger;

    public ElasticConnectivityHealthCheck(
        IElasticHealthService elasticHealthService,
        ILogger<ElasticConnectivityHealthCheck> logger)
    {
        _elasticHealthService = elasticHealthService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isAvailable = await _elasticHealthService.IsElasticAvailableAsync(cancellationToken);

        if (isAvailable)
        {
            return HealthCheckResult.Healthy("Elasticsearch is reachable.");
        }

        _logger.LogWarning("Elasticsearch is not reachable during health check.");
        return HealthCheckResult.Unhealthy("Elasticsearch ping failed.");
    }
}
