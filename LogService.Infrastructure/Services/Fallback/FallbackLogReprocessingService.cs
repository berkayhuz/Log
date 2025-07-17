namespace LogService.Infrastructure.Services.Fallback;
using System;
using System.Text.Json;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Fallback;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Resilience;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;

using Microsoft.Extensions.Hosting;

public class FallbackLogReprocessingService(
    IFallbackLogWriter fallbackWriter,
    ILogEntryWriteService directWriter,
    IResilientLogWriter resilientWriter,
    IElasticHealthService elasticHealthService,
    ILogServiceLogger logger,
    IFallbackProcessingStateService stateService) : BackgroundService
{
    private readonly string _fallbackFolder = Path.Combine(Directory.GetCurrentDirectory(), "FallbackLogs");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string className = nameof(FallbackLogReprocessingService);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = stateService.Current;
            bool elasticUp = await elasticHealthService.IsElasticAvailableAsync(stoppingToken);

            if (!elasticUp)
            {
                await logger.LogAsync(LogStage.Warning, "ElasticSearch ağı erişilemez durumda. Fallback modları çalıştırılıyor.");

                if (options.EnableResilient)
                {
                    await ProcessWithResilientWriter(stoppingToken);
                }

                if (options.EnableDirect)
                {
                    await ProcessWithDirectWriter(stoppingToken);
                }

                if (options.EnableRetry)
                {
                    await fallbackWriter.RetryPendingAsync(stoppingToken);
                }
            }
            else
            {
                await logger.LogAsync(LogStage.Information, "ElasticSearch ağı sağlıklı. Fallback işleme durduruldu.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessWithResilientWriter(CancellationToken stoppingToken)
    {
        const string className = nameof(FallbackLogReprocessingService);

        if (!Directory.Exists(_fallbackFolder))
        {
            Directory.CreateDirectory(_fallbackFolder);
        }

        var files = Directory.GetFiles(_fallbackFolder, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, stoppingToken);
                var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

                if (dto is null)
                {
                    continue;
                }

                var result = await resilientWriter.WriteWithRetryAsync(dto, stoppingToken);

                File.Delete(file);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(LogStage.Warning, $"[ResilientWrite] Dosya işlenemedi: {ex.Message}");
            }
        }
    }

    private async Task ProcessWithDirectWriter(CancellationToken stoppingToken)
    {
        const string className = nameof(FallbackLogReprocessingService);

        var files = fallbackWriter.GetPendingFiles();
        var count = files.Count();

        foreach (var file in files)
        {
            try
            {
                var dto = await fallbackWriter.ReadAsync(file);

                if (dto is null)
                {
                    continue;
                }

                var result = await directWriter.WriteToElasticAsync(dto);

                if (result.IsSuccess)
                {
                    fallbackWriter.Delete(file);
                }
                else
                {
                    await logger.LogAsync(LogStage.Warning, $"[DirectWrite] Tekrar yazılamadı: {string.Join(", ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                await logger.LogAsync(LogStage.Warning, $"[DirectWrite] Hata oluştu: {ex.Message}");
            }
        }
    }
}
