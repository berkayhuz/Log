namespace LogService.Infrastructure.Services.Elastic;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Clients.Elasticsearch.Core.Search;
using global::Elastic.Clients.Elasticsearch.Core.TermVectors;
using global::Elastic.Clients.Elasticsearch.QueryDsl;
using global::Elastic.Transport;
using global::Elastic.Transport.Extensions;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Features.DTOs;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

public class ElasticLogClient(ElasticsearchClient client, ILogServiceLogger logLogger) : IElasticLogClient
{
    public async Task<Result<FlexibleLogQueryResult>> QueryLogsFlexibleAsync(
        string indexName,
        LogFilterDto filter,
        List<LogStage> allowedLevels,
        bool fetchCount = false,
        bool fetchDocuments = true,
        List<string>? includeFields = null)
    {
        const string className = nameof(ElasticLogClient);

        var request = BuildSearchRequest(indexName, filter, allowedLevels, fetchCount, fetchDocuments, includeFields);

        try
        {
            // DEBUG: request json log
            var requestJson = client.RequestResponseSerializer.SerializeToString(
                request,
                formatting: SerializationFormatting.Indented
            );

            var response = await client.SearchAsync<LogEntryDto>(request);

            if (!response.IsValidResponse)
            {
                var reason = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown reason";
                var details = response.ApiCallDetails?.ToString() ?? "No call details";

                await logLogger.LogAsync(
                    LogStage.Error,
                    $"Elastic sorgu hatası (Index: {indexName}): {reason}\nDetails: {details}",
                    new Exception(reason)
                );

                return Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu başarısız", reason);
            }

            var result = new FlexibleLogQueryResult
            {
                TotalCount = fetchCount ? GetTotalCount(response.HitsMetadata?.Total) : null,
                Documents = fetchDocuments ? response.Documents.ToList() : null
            };

            return Result<FlexibleLogQueryResult>.Success(result);
        }
        catch (Exception ex)
        {
            await logLogger.LogAsync(
                LogStage.Error,
                $"Elastic sorgu hatası (Exception): {ex.Message}",
                ex
            );

            return Result<FlexibleLogQueryResult>.Failure("Elastic sorgusu başarısız", ex.Message);
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
        List<LogStage> allowedLevels,
        bool fetchCount,
        bool fetchDocuments,
        List<string>? includeFields)
    {
        const string className = nameof(ElasticLogClient);

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
