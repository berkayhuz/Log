namespace LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using LogService.Application.Abstractions.Requests;
using LogService.Application.Features.DTOs;
using LogService.Application.Options;
using LogService.Domain.Constants;
using LogService.Domain.DTOs;

using MediatR;

using SharedKernel.Common.Results;

public class QueryLogsFlexible : IRequest<Result<FlexibleLogQueryResult>>, ICachableRequest
{
    public string IndexName { get; set; } = LogConstants.DefaultIndexWildcard;

    public LogFilterDto Filter { get; set; } = new();

    public FetchOptions Options { get; set; } = new();

    public string CacheKey =>
        $"log:flexible:{IndexName}:{Filter.StartDate:yyyyMMdd}:{Filter.EndDate:yyyyMMdd}:{Filter.Page}:{Filter.PageSize}";

    public TimeSpan? Expiration => TimeSpan.FromMinutes(3);

    public string? CacheRegion => $"region:{IndexName}";
}
