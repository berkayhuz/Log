namespace SharedKernel.Http.Abstractions;
public interface IRequestContextService
{
    string IpAddress { get; }
    string Device { get; }
    string? CorrelationId { get; }

    void SetContext(string ipAddress, string device, string? correlationId = null);
}
