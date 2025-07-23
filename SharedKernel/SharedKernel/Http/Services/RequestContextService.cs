namespace SharedKernel.Http.Services;

using SharedKernel.Http.Abstractions;

public class RequestContextService : IRequestContextService
{
    public string IpAddress { get; private set; } = "unknown";
    public string Device { get; private set; } = "unknown";
    public string? CorrelationId { get; private set; }

    public void SetContext(string ipAddress, string device, string? correlationId = null)
    {
        IpAddress = ipAddress;
        Device = device;
        CorrelationId = correlationId;
    }
}
