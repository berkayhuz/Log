namespace LogService.Application.Abstractions.Elastic;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IElasticIndexService
{
    Task<List<string>> GetIndexNamesAsync(CancellationToken cancellationToken = default);
}
