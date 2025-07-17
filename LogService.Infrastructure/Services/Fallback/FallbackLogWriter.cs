namespace LogService.Infrastructure.Services.Fallback;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Application.Abstractions.Logging;
using LogService.SharedKernel.DTOs;

public class FallbackLogWriter : IFallbackLogWriter
{
    private readonly string _directoryPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "FallbackLogs");
    private readonly ILogEntryWriteService _logWriter;

    public FallbackLogWriter(ILogEntryWriteService logWriter)
    {
        const string className = nameof(FallbackLogWriter);
        _logWriter = logWriter;

        Directory.CreateDirectory(_directoryPath);
    }

    public async Task WriteAsync(LogEntryDto log)
    {
        const string className = nameof(FallbackLogWriter);
        var filePath = Path.Combine(_directoryPath, $"{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = false });

        await File.WriteAllTextAsync(filePath, json);
    }

    public IEnumerable<string> GetPendingFiles()
    {
        const string className = nameof(FallbackLogWriter);

        return Directory.EnumerateFiles(_directoryPath, "*.json");
    }

    public async Task<LogEntryDto?> ReadAsync(string path)
    {
        const string className = nameof(FallbackLogWriter);
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

            return dto;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public void Delete(string path)
    {
        const string className = nameof(FallbackLogWriter);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public async Task RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        const string className = nameof(FallbackLogWriter);
        var files = Directory.GetFiles(_directoryPath, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

                if (dto is null)
                {
                    continue;
                }

                var result = await _logWriter.WriteToElasticAsync(dto);

                if (result.IsSuccess)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}
