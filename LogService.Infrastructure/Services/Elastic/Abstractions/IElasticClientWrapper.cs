namespace LogService.Infrastructure.Services.Elastic.Abstractions;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

using LogService.Domain.DTOs;

public record PingResult(bool IsValidResponse);
public interface IElasticClientWrapper
{
    Task<PingResult> PingAsync(CancellationToken cancellationToken = default);

    Task<SearchResult<T>> SearchAsync<T>(SearchRequest<T> request, CancellationToken ct = default) where T : class;

}
