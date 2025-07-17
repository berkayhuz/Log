namespace LogService.Infrastructure.Services.Elastic;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using LogService.Application.Abstractions.Elastic;

public class ElasticIndexService : IElasticIndexService
{
    private readonly HttpClient _httpClient;

    public ElasticIndexService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<List<string>> GetIndexNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("_cat/indices?format=json", cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var indices = JsonSerializer.Deserialize<List<ElasticIndexResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return indices?.Select(i => i.Index).Distinct().ToList() ?? new();
        }
        catch
        {
            return new();
        }
    }

    private class ElasticIndexResponse
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;
    }
}
