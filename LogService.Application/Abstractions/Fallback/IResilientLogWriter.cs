namespace LogService.Application.Resilience;
using System.Threading.Tasks;

using LogService.Application.Common.Results;
using LogService.SharedKernel.DTOs;

public interface IResilientLogWriter
{
    Task<Result> WriteWithRetryAsync(LogEntryDto model, CancellationToken cancellationToken = default);
}
