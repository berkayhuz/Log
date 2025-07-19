namespace LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using System.Security.Claims;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;

using MediatR;

using Microsoft.AspNetCore.Http;

public class QueryLogsFlexibleHandler(
    ILogQueryService logQueryService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<QueryLogsFlexible, Result<FlexibleLogQueryResult>>
{
    public async Task<Result<FlexibleLogQueryResult>> Handle(QueryLogsFlexible request, CancellationToken cancellationToken)
    {
        var role = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "anonymous";
        var filterStr = System.Text.Json.JsonSerializer.Serialize(request.Filter);

        var result = await logQueryService.QueryLogsFlexibleAsync(
            request.IndexName,
            role,
            request.Filter,
            request.Options.FetchCount,
            request.Options.FetchDocuments,
            request.Options.IncludeFields
        );

        if (result.IsFailure)
        {
            return Result<FlexibleLogQueryResult>.Failure(result.Errors);
        }

        return Result<FlexibleLogQueryResult>.Success(result.Value);
    }
}

