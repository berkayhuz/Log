namespace LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.Application.Options;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;

using MediatR;

public class QueryLogsFlexible : IRequest<Result<FlexibleLogQueryResult>>
{
    public string IndexName { get; set; } = LogConstants.DefaultIndexWildcard;

    public LogFilterDto Filter { get; set; } = new();

    public FetchOptions Options { get; set; } = new();
}
