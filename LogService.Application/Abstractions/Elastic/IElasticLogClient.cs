namespace LogService.Application.Abstractions.Elastic;

using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

public interface IElasticLogClient
{
    Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        LogFilterDto filter,
        List<LogStage> allowedLevels,
        bool fetchCount = false,
        bool fetchDocuments = true,
        List<string>? includeFields = null);
}
