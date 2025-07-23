namespace LogService.Infrastructure.Services.Elastic.Clients;

using System.Threading;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Clients.Elasticsearch.Core.Search;
using global::Elastic.Clients.Elasticsearch.QueryDsl;

using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class ElasticLogClient : IElasticLogClient
{
    private readonly IElasticClientWrapper _client;
    private readonly ILogger<ElasticLogClient> _logger;

    public ElasticLogClient(IElasticClientWrapper client, ILogger<ElasticLogClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
    string indexName,
    LogFilterDto filter,
    List<ErrorLevel> allowedLevels,
    bool fetchCount = false,
    bool fetchDocuments = true,
    List<string>? includeFields = null,
    CancellationToken cancellationToken = default)
    {

        var request = BuildSearchRequest(indexName, filter, allowedLevels, fetchCount, fetchDocuments, includeFields);

        try
        {
            var response = await _client.SearchAsync(request, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogWarning("Elastic response not valid: {Reason}", response.ErrorReason);
                return Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu başarısız", response.ErrorReason ?? "Unknown");
            }


            var result = new FlexibleLogQueryResult
            {
                TotalCount = fetchCount ? response.TotalCount : null,
                Documents = fetchDocuments ? response.Documents.ToList() : null
            };

            return Result<FlexibleLogQueryResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch sorgusu sırasında beklenmeyen hata oluştu.");
            return Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu sırasında hata oluştu", ex.Message)
                .WithException(ex)
                .WithErrorType(ErrorType.Infrastructure);
        }
    }

    private static long? GetTotalCount(Union<TotalHits, long>? total)
    {
        return total?.Match(
            totalHits => totalHits.Value,
            longValue => longValue
        );
    }

    private SearchRequest<LogEntryDto> BuildSearchRequest(
        string indexName,
        LogFilterDto filter,
        List<ErrorLevel> allowedLevels,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields)
    {
        if (allowedLevels == null || allowedLevels.Count == 0)
            throw new ArgumentException("allowedLevels cannot be null or empty.", nameof(allowedLevels));

        int from = (filter.Page - 1) * filter.PageSize;

        return new SearchRequest<LogEntryDto>(indexName)
        {
            From = fetchDocuments ? from : null,
            Size = fetchDocuments ? filter.PageSize : 0,
            Query = new BoolQuery
            {
                Filter = new List<Query>
                {
                    new DateRangeQuery(new Field("@timestamp"))
                    {
                        Gte = filter.StartDate,
                        Lte = filter.EndDate
                    },
                    new TermsQuery
                    {
                        Field = new Field("log_level.keyword"),
                        Terms = new TermsQueryField(
                            allowedLevels.Select(l => FieldValue.String(l.ToString())).ToArray()
                        )
                    }
                }
            },
            Source = includeFields is { Count: > 0 }
                ? new SourceConfig(new SourceFilter
                {
                    Includes = includeFields.Select(f => (Field)f).ToArray()
                })
                : null,
            TrackTotalHits = fetchCount ? new TrackHits(true) : null
        };
    }
}
