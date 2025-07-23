namespace LogService.Infrastructure.Services.Fallback.Writers;

using System.Text.Json;

using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Logging.Abstractions;

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
        try
        {
            var fileName = $"{Guid.NewGuid()}.json";
            var filePath = Path.Combine(_directoryPath, fileName);
            var json = JsonSerializer.Serialize(log);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("✅ Fallback log dosyaya yazıldı: {File}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fallback dosyası yazılamadı.");
        }
    }

    public IEnumerable<string> GetPendingFiles()
    {
        return Directory.EnumerateFiles(_directoryPath, "*.json");
    }

    public async Task<LogEntryDto?> ReadAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

            if (dto is null)
                _logger.LogWarning("⚠️ Fallback dosyası deserialize edilemedi: {Path}", path);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Fallback dosyası okunamadı: {Path}", path);
            return null;
        }
    }

    public void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("🧹 Fallback dosyası silindi: {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Fallback dosyası silinemedi: {Path}", path);
        }
    }

    public async Task RetryPendingAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

                if (dto is null)
                {
                    _logger.LogWarning("⚠️ Retry: Geçersiz DTO: {File}", file);
                    continue;
                }

                var result = await _logWriter.WriteToElasticAsync(dto);

                if (result.IsSuccess)
                {
                    File.Delete(file);
                    _logger.LogInformation("✅ Retry başarılı ve dosya silindi: {File}", file);
                }
                else
                {
                    _logger.LogWarning(
                        "❌ Retry başarısız: {File} | Errors: {Errors}",
                        file, string.Join("; ", result.Errors)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Retry sırasında hata oluştu: {File}", file);
            }
        }
    }
}
