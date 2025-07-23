namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Fallback.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Objects;

[Name("fallback_disk_write_read")]
[HealthTags("fallback", "disk", "io", "log")]
public class FallbackWriteReadHealthCheck : IHealthCheck
{
    private readonly IFallbackLogWriter _fallbackWriter;
    private readonly ILogger<FallbackWriteReadHealthCheck> _logger;

    public FallbackWriteReadHealthCheck(
        IFallbackLogWriter fallbackWriter,
        ILogger<FallbackWriteReadHealthCheck> logger)
    {
        _fallbackWriter = fallbackWriter;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var testLog = new LogEntryDto
        {
            Timestamp = DateTime.UtcNow,
            Message = "FallbackHealthCheck-Test",
            Level = ErrorLevel.Information
        };

        string? testFile = null;

        try
        {
            testFile = Path.Combine(AppContext.BaseDirectory, "App_Data", "FallbackLogs", $"healthcheck-test-{Guid.NewGuid()}.json");
            var json = JsonSerializer.Serialize(testLog);
            await File.WriteAllTextAsync(testFile, json, cancellationToken);

            var readJson = await File.ReadAllTextAsync(testFile, cancellationToken);
            var readDto = JsonSerializer.Deserialize<LogEntryDto>(readJson);

            if (readDto is null || readDto.Message != testLog.Message)
            {
                return HealthCheckResult.Degraded("Fallback write/read test başarısız: İçerik eşleşmiyor.");
            }

            return HealthCheckResult.Healthy("Fallback diske yazma/okuma başarılı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback write/read işlemi sırasında hata.");
            return HealthCheckResult.Unhealthy("Fallback diske yazma/okuma hatası: " + ex.Message);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(testFile) && File.Exists(testFile))
            {
                try { File.Delete(testFile); } catch { }
            }
        }
    }
}
