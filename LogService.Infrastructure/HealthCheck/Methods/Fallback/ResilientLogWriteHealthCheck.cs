namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Application.Resilience;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

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
            Level = LogSeverityCode.Information
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
