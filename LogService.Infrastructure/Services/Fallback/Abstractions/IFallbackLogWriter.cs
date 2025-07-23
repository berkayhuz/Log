namespace LogService.Infrastructure.Services.Fallback.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

using LogService.Domain.DTOs;

public interface IFallbackLogWriter
{
    Task WriteAsync(LogEntryDto log);
    IEnumerable<string> GetPendingFiles();
    Task<LogEntryDto?> ReadAsync(string path);
    void Delete(string path);
    Task RetryPendingAsync(CancellationToken cancellationToken = default);
}

