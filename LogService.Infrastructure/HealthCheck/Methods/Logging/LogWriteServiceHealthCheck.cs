namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Threading.Tasks;

using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Objects;

[Name("log_write_service")]
[HealthTags("elastic", "log", "write")]
public class LogWriteServiceHealthCheck : IHealthCheck
{
    private readonly ILogEntryWriteService _logWriter;
    private readonly ILogger<LogWriteServiceHealthCheck> _logger;

    public LogWriteServiceHealthCheck(
        ILogEntryWriteService logWriter,
        ILogger<LogWriteServiceHealthCheck> logger)
    {
        _logWriter = logWriter;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dummyLog = new LogEntryDto
            {
                Message = "HealthCheck::LogWriteService",
                Level = ErrorLevel.Debug,
                TraceId = "healthcheck-trace",
                IpAddress = "127.0.0.1",
                Timestamp = DateTime.UtcNow,
                Code = "AAAA-LOG-WRITE"
            };

            var result = await _logWriter.WriteToElasticAsync(dummyLog);

            return result.IsSuccess
                ? HealthCheckResult.Healthy("Log yazımı başarılı.")
                : HealthCheckResult.Degraded($"Log yazımı başarısız: {string.Join(";", result.Errors)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogWriteService sağlık kontrolü sırasında hata oluştu.");
            return HealthCheckResult.Unhealthy("Log yazımı sırasında exception fırladı.", ex);
        }
    }
}
