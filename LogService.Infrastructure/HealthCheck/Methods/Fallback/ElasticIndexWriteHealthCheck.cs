namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Fallback.RetryPolicies;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("elasticsearch_index_retry")]
[HealthTags("elastic", "retry", "fallback", "resilience")]
public class ElasticIndexWriteHealthCheck : IHealthCheck
{
    private readonly IElasticClientAdapter _elasticAdapter;
    private readonly ILogger<ElasticIndexWriteHealthCheck> _logger;

    public ElasticIndexWriteHealthCheck(
        IElasticClientAdapter elasticAdapter,
        ILogger<ElasticIndexWriteHealthCheck> logger)
    {
        _elasticAdapter = elasticAdapter;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var testIndex = "logservice-healthcheck-test";
        var testDoc = new
        {
            timestamp = DateTime.UtcNow,
            message = "healthcheck-elastic-retry",
            level = "Info",
            meta = new { hc = true }
        };

        try
        {
            var request = new IndexRequest<object>(testIndex)
            {
                Document = testDoc,
                OpType = OpType.Create
            };

            var result = await PollyPolicies.RetryElasticPolicy.ExecuteAsync(() =>
                _elasticAdapter.IndexAsync(request));

            if (result.IsValidResponse)
            {
                return HealthCheckResult.Healthy("Elastic Polly retry başarılı.");
            }

            _logger.LogWarning("Elastic index yazımı başarısız. Hata: {Error}", result.ErrorReason);

            return HealthCheckResult.Degraded("Polly retry sonrası bile yazılamadı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elastic Polly retry mekanizması hata fırlattı.");
            return HealthCheckResult.Unhealthy("Polly retry çalışırken exception oluştu: " + ex.Message);
        }
    }
}
