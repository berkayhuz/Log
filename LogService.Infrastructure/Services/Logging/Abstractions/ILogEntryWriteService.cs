namespace LogService.Infrastructure.Services.Logging.Abstractions;
using System.Threading.Tasks;

using LogService.Domain.DTOs;

using SharedKernel.Common.Results;

public interface ILogEntryWriteService
{
    Task<Result> WriteToElasticAsync(LogEntryDto model);
}
