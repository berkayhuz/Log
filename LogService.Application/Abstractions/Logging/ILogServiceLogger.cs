namespace LogService.Application.Abstractions.Logging;
using System;
using System.Threading.Tasks;

using LogService.SharedKernel.Enums;

public interface ILogServiceLogger
{
    Task LogAsync(LogStage level, string message, Exception? exception = null);
}
