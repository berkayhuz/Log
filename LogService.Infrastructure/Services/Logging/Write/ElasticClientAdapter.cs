namespace LogService.Infrastructure.Services.Logging.Write;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Infrastructure.Services.Logging.Abstractions;

public class ElasticClientAdapter : IElasticClientAdapter
{
    private readonly ElasticsearchClient _client;

    public ElasticClientAdapter(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<IElasticResponseWrapper> IndexAsync<T>(IndexRequest<T> request) where T : class
    {
        var response = await _client.IndexAsync(request);
        return new ElasticResponseWrapper(response);
    }

    private class ElasticResponseWrapper : IElasticResponseWrapper
    {
        private readonly IndexResponse _response;

        public ElasticResponseWrapper(IndexResponse response)
        {
            _response = response;
        }

        public bool IsValidResponse => _response.IsValidResponse;
        public string? ErrorReason => _response.ElasticsearchServerError?.Error?.Reason;
    }
}
