namespace LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using System.Security.Claims;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Features.DTOs;
using LogService.Domain.Constants;

using MediatR;

using Microsoft.AspNetCore.Http;

using SharedKernel.Common.Results;

public class QueryLogsFlexibleHandler(
    ILogQueryService logQueryService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<QueryLogsFlexible, Result<FlexibleLogQueryResult>>
{
    public async Task<Result<FlexibleLogQueryResult>> Handle(QueryLogsFlexible request, CancellationToken cancellationToken)
    {
        var role = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous";

        var indexName = string.IsNullOrWhiteSpace(request.IndexName)
            ? LogConstants.DefaultIndexWildcard
            : request.IndexName;

        return await logQueryService.QueryLogsFlexibleAsync(
            indexName: indexName,
            role: role,
            filter: request.Filter,
            fetchCount: request.Options.FetchCount,
            fetchDocuments: request.Options.FetchDocuments,
            includeFields: request.Options.IncludeFields
        );
    }
}
