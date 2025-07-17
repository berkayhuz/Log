namespace LogService.Application.Abstractions.Logging;

using LogService.Application.Common.Results;
using LogService.SharedKernel.DTOs;

public interface ILogEntryWriteService
{
    Task<Result> WriteToElasticAsync(LogEntryDto model);
}
