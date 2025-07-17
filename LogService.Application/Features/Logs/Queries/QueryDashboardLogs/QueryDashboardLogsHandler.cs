
namespace LogService.Application.Features.Logs.Queries.QueryDashboardLogs;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.Constants;

using MediatR;

public class QueryDashboardLogsHandler(ILogQueryService logService)
    : IRequestHandler<QueryDashboardLogs, Result<FlexibleLogQueryResult>>
{
    public async Task<Result<FlexibleLogQueryResult>> Handle(QueryDashboardLogs request, CancellationToken cancellationToken)
    {
        const string className = nameof(QueryDashboardLogsHandler);

        var indexName = string.IsNullOrWhiteSpace(request.IndexName)
            ? LogConstants.DataStreamName
            : request.IndexName;

        var result = await logService.QueryLogsFlexibleAsync(
            indexName: indexName,
            role: "Admin",
            filter: request.Filter,
            fetchCount: request.Options.FetchCount,
            fetchDocuments: request.Options.FetchDocuments,
            includeFields: request.Options.IncludeFields
        );

        return result;
    }
}
