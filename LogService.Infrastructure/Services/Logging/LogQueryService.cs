namespace LogService.Infrastructure.Services.Logging;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.Domain.Policies;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

public class LogQueryService(
    IElasticLogClient elasticClient) : ILogQueryService
{
    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        string role,
        LogFilterDto filter,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields = null)
    {
        var allowedLevels = RoleLogStageMap.GetAllowedLevels(role);

        if (!allowedLevels.Any())
        {
            return Result<FlexibleLogQueryResult>.Failure();
        }

        var allowedSeverityLevels = allowedLevels
            .Select(stage => (LogSeverityCode)(int)stage)
            .ToList();

        var result = await elasticClient.QueryLogsFlexibleAsync(
            indexName,
            filter,
            allowedSeverityLevels,
            fetchCount,
            fetchDocuments,
            includeFields);

        return result;
    }
}
