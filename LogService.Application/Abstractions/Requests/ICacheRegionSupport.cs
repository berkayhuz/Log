namespace LogService.Application.Abstractions.Requests;

public interface ICacheRegionSupport
{
    Task AddKeyToRegionAsync(string region, string key);
    Task InvalidateRegionAsync(string region);
}
