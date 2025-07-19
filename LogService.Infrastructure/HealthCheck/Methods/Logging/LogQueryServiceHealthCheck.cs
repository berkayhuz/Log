namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

[Name("log_query_service")]
[HealthTags("elastic", "log", "query")]
public class LogQueryServiceHealthCheck : IHealthCheck
{
    private readonly ILogQueryService _logQueryService;
    private readonly ILogger<LogQueryServiceHealthCheck> _logger;

    public LogQueryServiceHealthCheck(
        ILogQueryService logQueryService,
        ILogger<LogQueryServiceHealthCheck> logger)
    {
        _logQueryService = logQueryService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _logQueryService.QueryLogsFlexibleAsync(
                indexName: "logservice-logs",
                role: UserRole.Admin.ToString(),
                filter: new LogFilterDto
                {
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow,
                    Page = 1,
                    PageSize = 1
                },
                fetchCount: true,
                fetchDocuments: false
            );

            if (result.IsSuccess)
            {
                return HealthCheckResult.Healthy("Log sorgulama başarılı.");
            }

            return HealthCheckResult.Degraded("Log sorgulama başarısız.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogQueryService sağlık kontrolü sırasında hata oluştu.");
            return HealthCheckResult.Unhealthy("Exception fırladı.", ex);
        }
    }
}
