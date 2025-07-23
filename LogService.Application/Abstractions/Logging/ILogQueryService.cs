namespace LogService.Application.Abstractions.Logging;

using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;

using SharedKernel.Common.Results;

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
