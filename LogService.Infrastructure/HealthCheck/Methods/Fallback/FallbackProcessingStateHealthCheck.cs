namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("fallback_runtime_config")]
[HealthTags("fallback", "config", "runtime", "resilience")]
public class FallbackProcessingStateHealthCheck : IHealthCheck
{
    private readonly IFallbackProcessingStateService _stateService;
    private readonly ILogger<FallbackProcessingStateHealthCheck> _logger;

    public FallbackProcessingStateHealthCheck(
        IFallbackProcessingStateService stateService,
        ILogger<FallbackProcessingStateHealthCheck> logger)
    {
        _stateService = stateService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var config = _stateService.Current;

        var summary = $"resilient={config.EnableResilient}, direct={config.EnableDirect}, retry={config.EnableRetry}, interval={config.IntervalSeconds}s";

        if (!config.EnableResilient && !config.EnableDirect)
        {
            _logger.LogWarning("Fallback config: Hem Resilient hem Direct devre dışı!");
            return Task.FromResult(HealthCheckResult.Degraded($"Yapılandırma geçersiz: {summary}"));
        }

        if (config.IntervalSeconds <= 0)
        {
            _logger.LogWarning("Fallback config: Süre aralığı {Seconds} geçersiz!", config.IntervalSeconds);
            return Task.FromResult(HealthCheckResult.Degraded($"Süre aralığı geçersiz: {summary}"));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Fallback yapılandırması uygun: {summary}"));
    }
}
