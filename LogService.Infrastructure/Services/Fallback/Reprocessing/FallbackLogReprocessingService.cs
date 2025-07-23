namespace LogService.Infrastructure.Services.Fallback.Reprocessing;

using System.Threading.Channels;

using LogService.Application.Options;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class FallbackLogReprocessingService : BackgroundService
{
    private readonly Channel<string> _channel;
    private readonly IFallbackLogWriter _fallbackWriter;
    private readonly IElasticHealthService _elasticHealthService;
    private readonly IResilientLogWriter _resilientWriter;
    private readonly ILogEntryWriteService _directWriter;
    private readonly ILogger<FallbackLogReprocessingService> _logger;
    private readonly IOptionsMonitor<FallbackProcessingRuntimeOptions> _opts;
    private readonly string _folder;
    private readonly FileSystemWatcher _watcher;

    public FallbackLogReprocessingService(
        IFallbackLogWriter fallbackWriter,
        IElasticHealthService elasticHealthService,
        IResilientLogWriter resilientWriter,
        ILogEntryWriteService directWriter,
        IOptionsMonitor<FallbackProcessingRuntimeOptions> opts,
        ILogger<FallbackLogReprocessingService> logger)
    {
        _fallbackWriter = fallbackWriter;
        _elasticHealthService = elasticHealthService;
        _resilientWriter = resilientWriter;
        _directWriter = directWriter;
        _opts = opts;
        _logger = logger;

        _folder = Path.Combine(AppContext.BaseDirectory, "App_Data", "FallbackLogs");
        Directory.CreateDirectory(_folder);

        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _watcher = new FileSystemWatcher(_folder, "*.json")
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };
        _watcher.Created += (_, e) => _channel.Writer.TryWrite(e.FullPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load existing .json files on startup
        foreach (var file in Directory.EnumerateFiles(_folder, "*.json"))
        {
            await _channel.Writer.WriteAsync(file, stoppingToken);
        }

        await foreach (var filePath in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (!await IsElasticUp(stoppingToken, filePath))
                continue;

            try
            {
                var dto = await _fallbackWriter.ReadAsync(filePath);
                if (dto is null)
                {
                    _logger.LogWarning("⚠️ DTO yok/parse edilemedi: {File}", filePath);
                    _fallbackWriter.Delete(filePath);
                    continue;
                }

                var opt = _opts.CurrentValue;

                Result result = opt.EnableResilient
                    ? await _resilientWriter.WriteWithRetryAsync(dto, stoppingToken)
                    : await _directWriter.WriteToElasticAsync(dto);

                if (result.IsSuccess)
                {
                    _fallbackWriter.Delete(filePath);
                    _logger.LogInformation("✅ Fallback işlendi: {File}", filePath);
                }
                else
                {
                    _logger.LogWarning("❌ İşleme başarısız. Errors: {Errors}. Dosya bırakıldı: {File}",
                        string.Join("; ", result.Errors), filePath);

                    if (result.ErrorType == ErrorType.Infrastructure || result.ErrorLevel >= ErrorLevel.Error)
                    {
                        // retry logic genişletilebilir
                        // dosya bırakılır
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Fallback işlenirken beklenmeyen hata oluştu. File: {File}", filePath);
            }
        }
    }

    private async Task<bool> IsElasticUp(CancellationToken cancellationToken, string filePath)
    {
        var availability = await _elasticHealthService.IsElasticAvailableAsync(cancellationToken);
        if (!availability.IsSuccess || availability.Value is false)
        {
            _logger.LogWarning("⛔ Elastic DOWN. File bekletildi: {File}", filePath);
            return false;
        }

        return true;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = false;

        try
        {
            _watcher.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Watcher dispose sırasında hata");
        }

        return base.StopAsync(cancellationToken);
    }
}
