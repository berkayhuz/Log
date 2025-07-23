namespace LogService.Infrastructure.Services.Logging.Read;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;
using LogService.Domain.Policies;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class LogQueryService(IElasticLogClient elasticClient) : ILogQueryService
{
    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        string role,
        LogFilterDto filter,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields = null)
    {
        try
        {
            var allowedLevels = RoleLogStageMap.GetAllowedLevels(role);

            if (allowedLevels is null || allowedLevels.Count == 0)
            {
                return Result<FlexibleLogQueryResult>
                    .Failure("Bu role için log erişimi tanımlı değil.")
                    .WithStatusCode(StatusCodes.Forbidden)
                    .WithErrorCode(ErrorCode.AccessDenied)
                    .WithErrorType(ErrorType.Forbidden);
            }

            var allowedSeverityLevels = allowedLevels
                .Select(stage => (ErrorLevel)(int)stage)
                .ToList();

            var result = await elasticClient.QueryLogsFlexibleAsync(
                indexName,
                filter,
                allowedSeverityLevels,
                fetchCount,
                fetchDocuments,
                includeFields
            );

            return result;
        }
        catch (Exception ex)
        {
            return Result<FlexibleLogQueryResult>
                .Failure("Log sorgusu sırasında beklenmeyen bir hata oluştu.")
                .WithException(ex)
                .WithErrorType(ErrorType.Unexpected)
                .WithStatusCode(StatusCodes.InternalServerError);
        }
    }
}
