namespace LogService.Application.Abstractions.Logging;
public interface IBulkLogEntryWriteService : ILogEntryWriteService
{
    int PendingCount { get; }
}
