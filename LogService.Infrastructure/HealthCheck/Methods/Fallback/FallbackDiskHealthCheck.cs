namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("fallback_disk_queue")]
[HealthTags("fallback", "elastic", "disk", "resilience")]
public class FallbackDiskHealthCheck : IHealthCheck
{
    private readonly IElasticHealthService _elasticHealthService;
    private readonly ILogger<FallbackDiskHealthCheck> _logger;
    private readonly string _folderPath;

    public FallbackDiskHealthCheck(
        IElasticHealthService elasticHealthService,
        ILogger<FallbackDiskHealthCheck> logger)
    {
        _elasticHealthService = elasticHealthService;
        _logger = logger;
        _folderPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "FallbackLogs");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_folderPath))
            {
                return HealthCheckResult.Healthy("Fallback klasörü henüz oluşmamış (muhtemelen ilk çalıştırma).");
            }

            var pendingFiles = Directory.GetFiles(_folderPath, "*.json").Length;
            var isElasticUp = await _elasticHealthService.IsElasticAvailableAsync(cancellationToken);

            if (pendingFiles == 0)
            {
                return HealthCheckResult.Healthy("Tüm fallback logları işlenmiş.");
            }

            if (!isElasticUp)
            {
                _logger.LogWarning("Elastic DOWN iken {Count} fallback dosyası bekliyor.", pendingFiles);
                return HealthCheckResult.Degraded($"Elastic down, {pendingFiles} dosya bekliyor.");
            }

            if (pendingFiles > 100)
            {
                _logger.LogWarning("{Count} adet fallback log dosyası bekliyor.", pendingFiles);
                return HealthCheckResult.Degraded($"{pendingFiles} adet işlenmemiş fallback log var.");
            }

            return HealthCheckResult.Healthy($"{pendingFiles} adet işlenmeyi bekleyen fallback log var.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback klasör kontrolü sırasında hata oluştu.");
            return HealthCheckResult.Unhealthy("Fallback klasör kontrolü sırasında hata oluştu: " + ex.Message);
        }
    }
}
