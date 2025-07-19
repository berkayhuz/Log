namespace LogService.Infrastructure.Services.Fallback;
using System;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Fallback;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Options;
using LogService.Application.Resilience;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private FileSystemWatcher _watcher;

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
        _watcher.Created += (s, e) => _channel.Writer.TryWrite(e.FullPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var f in Directory.EnumerateFiles(_folder, "*.json"))
            await _channel.Writer.WriteAsync(f, stoppingToken);

        await foreach (var filePath in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (!await _elasticHealthService.IsElasticAvailableAsync(stoppingToken))
            {
                _logger.LogWarning("Elastic down, işlenemedi: {File}", filePath);
                continue;
            }

            await TryCatch.ExecuteAsync(
                tryFunc: async () =>
                {
                    var dto = await _fallbackWriter.ReadAsync(filePath);
                    if (dto is null)
                    {
                        _logger.LogWarning("DTO yok/parse edilemedi: {File}", filePath);
                        _fallbackWriter.Delete(filePath);
                        return;
                    }

                    var opt = _opts.CurrentValue;
                    var result = opt.EnableResilient
                        ? await _resilientWriter.WriteWithRetryAsync(dto, stoppingToken)
                        : await _directWriter.WriteToElasticAsync(dto);

                    if (result.IsSuccess)
                    {
                        _fallbackWriter.Delete(filePath);
                        _logger.LogInformation("Fallback işlendi: {File}", filePath);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "İşleme başarısız ({Errors}), dosya bırakıldı: {File}",
                            string.Join(';', result.Errors), filePath);
                    }
                },
                logger: _logger,
                context: $"Fallback işlem: {Path.GetFileName(filePath)}"
            );
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
