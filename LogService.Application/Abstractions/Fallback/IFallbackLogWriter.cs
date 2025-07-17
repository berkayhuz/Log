namespace LogService.Application.Abstractions.Fallback;
using System.Collections.Generic;
using System.Threading.Tasks;

using LogService.SharedKernel.DTOs;

public interface IFallbackLogWriter
{
    Task WriteAsync(LogEntryDto log);
    IEnumerable<string> GetPendingFiles();
    Task<LogEntryDto?> ReadAsync(string path);
    void Delete(string path);
    Task RetryPendingAsync(CancellationToken cancellationToken = default);
}
