namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;

using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Elastic.Abstractions;

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
                return HealthCheckResult.Healthy("Fallback klasörü henüz oluşmamış (muhtemelen ilk başlatma).");
            }

            var pendingFiles = Directory.GetFiles(_folderPath, "*.json").Length;

            // Elastic sağlığını kontrol et
            var result = await _elasticHealthService.IsElasticAvailableAsync(cancellationToken);
            var isElasticUp = result.IsSuccess && result.Value;

            if (pendingFiles == 0)
            {
                return HealthCheckResult.Healthy("Tüm fallback logları başarıyla işlenmiş.");
            }

            if (!isElasticUp)
            {
                _logger.LogWarning("🔴 Elastic DOWN iken {Count} fallback dosyası bekliyor.", pendingFiles);
                return HealthCheckResult.Degraded($"Elastic şu anda kapalı, {pendingFiles} dosya kuyrukta.");
            }

            if (pendingFiles > 100)
            {
                _logger.LogWarning("⚠️ {Count} adet fallback log dosyası birikmiş.", pendingFiles);
                return HealthCheckResult.Degraded($"{pendingFiles} adet fallback log işlenmeyi bekliyor.");
            }

            if (pendingFiles > 1000)
                return HealthCheckResult.Unhealthy("Fallback kuyruğu kritik seviyede!");

            return HealthCheckResult.Healthy($"{pendingFiles} adet işlenmeyi bekleyen fallback log var.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🚨 Fallback klasör durumu kontrolü sırasında hata oluştu.");
            return HealthCheckResult.Unhealthy($"Fallback klasör kontrol hatası: {ex.Message}");
        }
    }
}
