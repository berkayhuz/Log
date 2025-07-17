namespace LogService.Application.Abstractions.Elastic;
using System.Threading.Tasks;

public interface IElasticHealthService
{
    Task<bool> IsElasticAvailableAsync(CancellationToken cancellationToken = default);
}
