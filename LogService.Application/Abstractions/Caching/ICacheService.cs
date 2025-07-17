namespace LogService.Application.Abstractions.Caching;
using System;
using System.Threading.Tasks;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan duration);
}

