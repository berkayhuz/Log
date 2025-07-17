namespace LogService.Application.Abstractions.Messages;
public interface ILogMessageProvider
{
    string Get(string key, string defaultMessage = null);
    IEnumerable<string> GetKeys();
    Task AddKeyAsync(string key);
}
