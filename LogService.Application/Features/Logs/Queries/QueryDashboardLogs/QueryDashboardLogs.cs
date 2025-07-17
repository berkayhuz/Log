namespace LogService.Application.Features.Logs.Queries.QueryDashboardLogs;
using System;

using LogService.Application.Abstractions.Requests;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.Application.Options;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;

using MediatR;

public class QueryDashboardLogs : IRequest<Result<FlexibleLogQueryResult>>, ICachableRequest
{
    public LogFilterDto Filter { get; set; } = new();
    public FetchOptions Options { get; set; } = new();

    // Artık wildcard yok, data stream adı net: logservice-logs
    public string IndexName { get; set; } = LogConstants.DataStreamName;

    public string CacheKey =>
        $"log:dashboard:{IndexName}:{Filter.StartDate:yyyyMMdd}:{Filter.EndDate:yyyyMMdd}:{Filter.Page}:{Filter.PageSize}";

    public TimeSpan? Expiration => TimeSpan.FromMinutes(3);

    public string? CacheRegion => $"region:{IndexName}";
}

