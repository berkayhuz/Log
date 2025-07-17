namespace LogService.Infrastructure.Services.Logging;
using System;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Resilience;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

public class LogServiceLogger(IResilientLogWriter logWriter) : ILogServiceLogger
{
    public async Task LogAsync(LogStage level, string message, Exception? exception = null)
    {
        const string className = nameof(LogServiceLogger);

        var log = new LogEntryDto
        {
            Level = level,
            Message = message,
            Exception = exception?.ToString(),
            Source = "LogService",
            Role = UserRole.Admin.ToString(),
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await logWriter.WriteWithRetryAsync(log);
        }
        catch (Exception ex)
        {
            return;
        }
    }
}

