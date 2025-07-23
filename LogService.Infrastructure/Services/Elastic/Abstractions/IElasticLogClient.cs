namespace LogService.Infrastructure.Services.Elastic.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using SharedKernel.Enums;

public interface IElasticLogClient
{
    Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        LogFilterDto filter,
        List<ErrorLevel> allowedLevels,
        bool fetchCount = false,
        bool fetchDocuments = true,
        List<string>? includeFields = null,
        CancellationToken cancellationToken = default);
}
