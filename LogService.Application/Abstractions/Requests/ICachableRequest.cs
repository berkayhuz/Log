namespace LogService.Application.Abstractions.Requests;
using System;

public interface ICachableRequest
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
    string? CacheRegion => null;
}
