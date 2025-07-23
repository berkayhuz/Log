namespace LogService.Infrastructure.Services.Elastic.Abstractions;
using System.Threading.Tasks;

using SharedKernel.Common.Results;
public interface IElasticHealthService
{
    Task<Result<bool>> IsElasticAvailableAsync(CancellationToken cancellationToken = default);
}

