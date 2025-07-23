namespace LogService.Infrastructure.Services.Fallback.Abstractions;
using System.Threading.Tasks;

using LogService.Domain.DTOs;

using SharedKernel.Common.Results;

public interface IResilientLogWriter
{
    Task<Result> WriteWithRetryAsync(LogEntryDto model, CancellationToken cancellationToken = default);
}
