namespace LogService.Infrastructure.Services.Logging;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.Domain.Policies;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

public class LogQueryService(IElasticLogClient elasticClient, ILogServiceLogger logLogger) : ILogQueryService
{
    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        string role,
        LogFilterDto filter,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields = null)
    {
        const string className = nameof(LogQueryService);

        var allowedLevels = RoleLogStageMap.GetAllowedLevels(role);

        if (!allowedLevels.Any())
        {
            var warning = LogMessageDefaults.Messages[LogMessageKeys.Auth_UnauthorizedLogQuery].Replace("{Role}", role);
            await logLogger.LogAsync(LogStage.Warning, warning);

            var userMessage = LogMessageDefaults.Messages[LogMessageKeys.Auth_RoleNotAuthorized].Replace("{Role}", role);
            return Result<FlexibleLogQueryResult>.Failure(userMessage);
        }

        var result = await elasticClient.QueryLogsFlexibleAsync(
            indexName,
            filter,
            allowedLevels.ToList(),
            fetchCount,
            fetchDocuments,
            includeFields);

        return result;
    }
}
