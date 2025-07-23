namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;

using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Elastic.Abstractions;

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
        var result = await _elasticHealthService.IsElasticAvailableAsync(cancellationToken);

        if (result.IsSuccess && result.Value)
        {
            return HealthCheckResult.Healthy("Elasticsearch is reachable.");
        }

        _logger.LogWarning("Elasticsearch is not reachable during health check. Reason: {Reason}", string.Join(" | ", result.Errors));
        return HealthCheckResult.Unhealthy("Elasticsearch ping failed.");
    }
}
