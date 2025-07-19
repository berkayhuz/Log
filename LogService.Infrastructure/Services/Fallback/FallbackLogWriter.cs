namespace LogService.Infrastructure.Services.Fallback;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Application.Abstractions.Logging;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

public class FallbackLogWriter : IFallbackLogWriter
{
    private readonly string _directoryPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "FallbackLogs");
    private readonly ILogEntryWriteService _logWriter;
    private readonly ILogger<FallbackLogWriter> _logger;

    public FallbackLogWriter(ILogEntryWriteService logWriter, ILogger<FallbackLogWriter> logger)
    {
        _logWriter = logWriter;
        _logger = logger;

        Directory.CreateDirectory(_directoryPath);
    }

    public async Task WriteAsync(LogEntryDto log)
    {
        var filePath = Path.Combine(_directoryPath, $"{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = false });

        await File.WriteAllTextAsync(filePath, json);
    }

    public IEnumerable<string> GetPendingFiles()
    {
        return Directory.EnumerateFiles(_directoryPath, "*.json");
    }

    public async Task<LogEntryDto?> ReadAsync(string path)
    {
        return await TryCatch.ExecuteAsync(
            tryFunc: async () =>
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<LogEntryDto>(json);
            },
            catchFunc: ex =>
            {
                _logger.LogWarning(ex, "Fallback dosyası okunamadı: {Path}", path);
                return Task.FromResult<LogEntryDto?>(null);
            },
            logger: _logger,
            context: $"FallbackLogWriter.ReadAsync({Path.GetFileName(path)})"
        );
    }

    public void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback dosyası silinemedi: {Path}", path);
        }
    }

    public async Task RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");

        foreach (var file in files)
        {
            await TryCatch.ExecuteAsync<bool>(
                tryFunc: async () =>
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

                    if (dto is null)
                    {
                        _logger.LogWarning("Retry: Geçersiz DTO: {File}", file);
                        return false;
                    }

                    var result = await _logWriter.WriteToElasticAsync(dto);
                    if (result.IsSuccess)
                    {
                        File.Delete(file);
                        _logger.LogInformation("Retry başarılı: {File}", file);
                    }
                    else
                    {
                        _logger.LogWarning("Retry başarısız: {File}, Errors: {Errors}", file, string.Join(';', result.Errors));
                    }

                    return true;
                },
                catchFunc: ex =>
                {
                    _logger.LogError(ex, "RetryPendingAsync sırasında hata: {File}", file);
                    return Task.FromResult(false);
                },
                logger: _logger,
                context: $"FallbackLogWriter.RetryPending({Path.GetFileName(file)})"
            );
        }
    }
}

