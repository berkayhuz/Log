namespace LogService.Infrastructure.Services.Elastic;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Clients.Elasticsearch.Core.Search;
using global::Elastic.Clients.Elasticsearch.QueryDsl;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

public class ElasticLogClient : IElasticLogClient
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticLogClient> _logger;

    public ElasticLogClient(ElasticsearchClient client, ILogger<ElasticLogClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        LogFilterDto filter,
        List<LogSeverityCode> allowedLevels,
        bool fetchCount = false,
        bool fetchDocuments = true,
        List<string>? includeFields = null)
    {
        var request = BuildSearchRequest(indexName, filter, allowedLevels, fetchCount, fetchDocuments, includeFields);

        return await TryCatch.ExecuteAsync<Result<FlexibleLogQueryResult>>(
            tryFunc: async () =>
            {
                var response = await _client.SearchAsync<LogEntryDto>(request);

                if (!response.IsValidResponse)
                {
                    var reason = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown reason";
                    var details = response.ApiCallDetails?.ToString() ?? "No call details";

                    return Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu başarısız", reason);
                }

                var result = new FlexibleLogQueryResult
                {
                    TotalCount = fetchCount ? GetTotalCount(response.HitsMetadata?.Total) : null,
                    Documents = fetchDocuments ? response.Documents.ToList() : null
                };

                return Result<FlexibleLogQueryResult>.Success(result);
            },
            catchFunc: ex =>
            {
                _logger.LogError(ex, "Elasticsearch sorgusu sırasında beklenmeyen hata oluştu.");
                return Task.FromResult(Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu başarısız", ex.Message));
            },
            logger: _logger,
            context: "ElasticLogClient.QueryLogsFlexibleAsync"
        );
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
        List<LogSeverityCode> allowedLevels,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields)
    {
        int from = (filter.Page - 1) * filter.PageSize;

        var request = new SearchRequest<LogEntryDto>(indexName)
        {
            From = fetchDocuments ? from : null,
            Size = fetchDocuments ? filter.PageSize : 0,
            Query = new BoolQuery
            {
                Filter = new List<Query>
                {
                    new DateRangeQuery(new Field("timestamp"))
                    {
                        Gte = filter.StartDate,
                        Lte = filter.EndDate
                    },
                    new TermsQuery
                    {
                        Field = new Field("level"),
                        Terms = new TermsQueryField(
                            allowedLevels.Select(l => FieldValue.Long((int)l)).ToArray()
                        )
                    }
                }
            },
            Source = includeFields is { Count: > 0 }
                ? new SourceConfig(new SourceFilter
                {
                    Includes = includeFields.ToArray()
                })
                : null,
            TrackTotalHits = fetchCount ? new TrackHits(true) : null
        };

        return request;
    }
}
