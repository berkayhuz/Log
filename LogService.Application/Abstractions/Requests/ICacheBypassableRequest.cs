namespace LogService.Application.Abstractions.Requests;

public interface ICacheBypassableRequest
{
    bool BypassCache { get; }
}
