namespace LogService.Infrastructure.Services.Elastic;

using System.Threading;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Application.Abstractions.Elastic;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

public class ElasticHealthService : IElasticHealthService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticHealthService> _logger;

    public ElasticHealthService(ElasticsearchClient client, ILogger<ElasticHealthService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> IsElasticAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await TryCatch.ExecuteAsync<bool>(
            tryFunc: async () =>
            {
                var response = await _client.PingAsync(cancellationToken);
                return response.IsValidResponse;
            },
            catchFunc: ex =>
            {
                _logger.LogWarning(ex, "Elasticsearch sağlık kontrolü başarısız.");
                return Task.FromResult(false);
            },
            logger: _logger,
            context: "ElasticHealthService.IsElasticAvailableAsync"
        );
    }
}
