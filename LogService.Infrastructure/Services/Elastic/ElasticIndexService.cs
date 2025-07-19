namespace LogService.Infrastructure.Services.Elastic;
using System.Text.Json;
using System.Text.Json.Serialization;

using LogService.Application.Abstractions.Elastic;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

public class ElasticIndexService : IElasticIndexService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticIndexService> _logger;

    public ElasticIndexService(HttpClient httpClient, ILogger<ElasticIndexService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<string>> GetIndexNamesAsync(CancellationToken cancellationToken = default)
    {
        return await TryCatch.ExecuteAsync<List<string>>(
            tryFunc: async () =>
            {
                var response = await _httpClient.GetAsync("_cat/indices?format=json", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var indices = JsonSerializer.Deserialize<List<ElasticIndexResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return indices?.Select(i => i.Index).Distinct().ToList() ?? new();
            },
            catchFunc: ex =>
            {
                _logger.LogWarning(ex, "Elasticsearch indeks isimleri alınamadı.");
                return Task.FromResult(new List<string>());
            },
            logger: _logger,
            context: "ElasticIndexService.GetIndexNamesAsync"
        );
    }

    private class ElasticIndexResponse
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;
    }
}
