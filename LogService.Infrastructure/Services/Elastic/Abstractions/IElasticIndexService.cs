namespace LogService.Infrastructure.Services.Elastic.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IElasticIndexService
{
    Task<List<string>> GetIndexNamesAsync(CancellationToken cancellationToken = default);
}
