namespace LogService.Infrastructure.Services.Logging.Abstractions;
public interface IBulkLogEntryWriteService : ILogEntryWriteService
{
    int PendingCount { get; }
}
