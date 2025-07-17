namespace LogService.Application.Abstractions.Logging;

using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.DTOs;

public interface ILogQueryService
{
    Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
       string indexName,
       string role,
       LogFilterDto filter,
       bool fetchCount,
       bool fetchDocuments,
       List<string>? includeFields = null);
}
