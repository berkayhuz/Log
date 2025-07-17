namespace LogService.Infrastructure.Services.Elastic;

using System.Threading;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Application.Abstractions.Elastic;

public class ElasticHealthService(ElasticsearchClient client) : IElasticHealthService
{
    public async Task<bool> IsElasticAvailableAsync(CancellationToken cancellationToken = default)
    {
        const string className = nameof(ElasticHealthService);

        try
        {
            var response = await client.PingAsync(cancellationToken);

            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
