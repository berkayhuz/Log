namespace LogService.Infrastructure.Services.Elastic.Clients;

using System.Threading;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Elastic.Abstractions;

public class ElasticClientWrapper : IElasticClientWrapper
{
    private readonly ElasticsearchClient _client;

    public ElasticClientWrapper(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<PingResult> PingAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.PingAsync(cancellationToken);
        return new PingResult(response.IsValidResponse);
    }

    public async Task<SearchResult<T>> SearchAsync<T>(SearchRequest<T> request, CancellationToken ct = default)
        where T : class
    {
        var response = await _client.SearchAsync<T>(request, ct);

        return new SearchResult<T>
        {
            IsValid = response.IsValidResponse,
            Documents = response.Documents?.ToList(),
            TotalCount = response.HitsMetadata?.Total?.Match(
                totalHits => totalHits.Value,
                longValue => longValue
            ),
            ErrorReason = response.ElasticsearchServerError?.Error?.Reason
        };
    }
}

