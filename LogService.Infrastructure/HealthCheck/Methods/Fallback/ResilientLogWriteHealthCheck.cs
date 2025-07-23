namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Fallback.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using SharedKernel.Common.Results.Objects;

[Name("resilient_log_writer")]
[HealthTags("elastic", "resilience", "fallback", "retry", "circuitbreaker")]
public class ResilientLogWriteHealthCheck : IHealthCheck
{
    private readonly IResilientLogWriter _resilientWriter;
    private readonly IFallbackLogWriter _fallbackWriter;

    public ResilientLogWriteHealthCheck(
        IResilientLogWriter resilientWriter,
        IFallbackLogWriter fallbackWriter)
    {
        _resilientWriter = resilientWriter;
        _fallbackWriter = fallbackWriter;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var log = new LogEntryDto
        {
            Timestamp = DateTime.UtcNow,
            Message = "healthcheck_resilient_writer",
            Level = ErrorLevel.Information
        };

        var result = await _resilientWriter.WriteWithRetryAsync(log, cancellationToken);

        if (result.IsSuccess)
        {
            return HealthCheckResult.Healthy("Resilient log yazımı başarılı.");
        }

        var fallbackExists = _fallbackWriter
            .GetPendingFiles()
            .Any(f => Path.GetFileName(f).Contains("healthcheck", StringComparison.OrdinalIgnoreCase));

        if (fallbackExists)
        {
            return HealthCheckResult.Degraded("Elastic başarısız ama fallback dosyası oluştu.");
        }

        return HealthCheckResult.Unhealthy("Log yazılamadı, fallback da başarısız.");
    }
}
