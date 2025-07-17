namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("fallback_disk_write_check")]
[HealthTags("fallback", "disk", "write", "file", "ready")]
public class FallbackDiskWriteHealthCheck(IFallbackLogWriter fallbackWriter) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testLog = new LogEntryDto
            {
                Level = LogStage.Information,
                Message = "Elastic write test log",
                Timestamp = DateTime.UtcNow
            };

            await fallbackWriter.WriteAsync(testLog);

            var pendingFiles = fallbackWriter.GetPendingFiles();

            foreach (var file in pendingFiles)
            {
                var dto = await fallbackWriter.ReadAsync(file);
                if (dto is not null && dto.Message == testLog.Message)
                {
                    fallbackWriter.Delete(file);
                    return HealthCheckResult.Healthy("Fallback disk write/read/delete succeeded.");
                }
            }

            return HealthCheckResult.Degraded("Fallback disk write succeeded but read/verify failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Fallback disk write/read failed.", ex);
        }
    }
}
