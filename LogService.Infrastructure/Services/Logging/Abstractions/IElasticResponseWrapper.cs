namespace LogService.Infrastructure.Services.Logging.Abstractions;
public interface IElasticResponseWrapper
{
    bool IsValidResponse { get; }
    string? ErrorReason { get; }
}
