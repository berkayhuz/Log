namespace LogService.Infrastructure.Services.Logging.Abstractions;
using System.Threading.Tasks;

using global::Elastic.Clients.Elasticsearch;

public interface IElasticClientAdapter
{
    Task<IElasticResponseWrapper> IndexAsync<T>(IndexRequest<T> request) where T : class;
}

