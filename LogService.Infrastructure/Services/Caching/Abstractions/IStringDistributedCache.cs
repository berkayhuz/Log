namespace LogService.Infrastructure.Services.Caching.Abstractions;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

public interface IStringDistributedCache
{
    Task<string?> GetStringAsync(string key, CancellationToken token = default);
    Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
}
