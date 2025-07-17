namespace LogService.Infrastructure.HealthCheck.Methods.Elastic;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("elastic_ping_check")]
[HealthTags("elastic", "ping", "ready")]
public sealed class ElasticPingHealthCheck(IElasticHealthService healthService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var available = await healthService.IsElasticAvailableAsync(cancellationToken);

        return available
            ? HealthCheckResult.Healthy("Elasticsearch is reachable.")
            : HealthCheckResult.Unhealthy("Elasticsearch ping failed.");
    }
}
