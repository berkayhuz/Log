namespace LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using System.Security.Claims;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using MediatR;

using Microsoft.AspNetCore.Http;

public class QueryLogsFlexibleHandler(
    ILogQueryService logQueryService,
    ILogServiceLogger logLogger,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<QueryLogsFlexible, Result<FlexibleLogQueryResult>>
{
    private readonly ILogServiceLogger _logLogger = logLogger;

    public async Task<Result<FlexibleLogQueryResult>> Handle(QueryLogsFlexible request, CancellationToken cancellationToken)
    {
        const string className = nameof(QueryLogsFlexibleHandler);

        var role = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "anonymous";
        var filterStr = System.Text.Json.JsonSerializer.Serialize(request.Filter);

        await _logLogger.LogAsync(
            LogStage.Debug,
            LogMessageDefaults.Messages[LogMessageKeys.Elastic_FlexibleQueryStarted]
                .Replace("{Index}", request.IndexName)
                .Replace("{Role}", role)
                .Replace("{Filter}", filterStr)
        );

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

            await _logLogger.LogAsync(
                LogStage.Warning,
                LogMessageDefaults.Messages[LogMessageKeys.Elastic_FlexibleQueryFailed]
                    .Replace("{Index}", request.IndexName)
                    .Replace("{Role}", role)
                    .Replace("{Errors}", string.Join("; ", result.Errors))
            );

            return Result<FlexibleLogQueryResult>.Failure(result.Errors);
        }

        await _logLogger.LogAsync(
            LogStage.Debug,
            LogMessageDefaults.Messages[LogMessageKeys.Elastic_FlexibleQuerySucceeded]
                .Replace("{Index}", request.IndexName)
                .Replace("{Role}", role)
                .Replace("{ReturnedCount}", (result.Value?.TotalCount ?? 0).ToString())
        );

        return Result<FlexibleLogQueryResult>.Success(result.Value);
    }
}
