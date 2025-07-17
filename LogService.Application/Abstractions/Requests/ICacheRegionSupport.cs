namespace LogService.Application.Abstractions.Requests;

public interface ICacheRegionSupport
{
    Task InvalidateRegionAsync(string region);
    Task AddKeyToRegionAsync(string region, string key);
}
