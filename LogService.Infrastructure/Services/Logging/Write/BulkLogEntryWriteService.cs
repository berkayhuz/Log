namespace LogService.Infrastructure.Services.Logging.Write;

using System.Threading.Channels;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Clients.Elasticsearch.Core.Bulk;

using LogService.Application.Options;
using LogService.Domain.Constants;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Common.Results.Objects;

using Result = SharedKernel.Common.Results.Result;

public class BulkLogEntryWriteService : BackgroundService, IBulkLogEntryWriteService
{
    private readonly ElasticsearchClient _client;
    private readonly BulkLogOptions _opts;
    private readonly Channel<LogEntryDto> _channel;
    private readonly ILogger<BulkLogEntryWriteService> _logger;

    public BulkLogEntryWriteService(
        ElasticsearchClient client,
        IOptions<BulkLogOptions> opts,
        ILogger<BulkLogEntryWriteService> logger)
    {
        _client = client;
        _opts = opts.Value;
        _logger = logger;

        var channelOpts = new BoundedChannelOptions(_opts.ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };

        _channel = Channel.CreateBounded<LogEntryDto>(channelOpts);
    }

    public Task<Result> WriteToElasticAsync(LogEntryDto model) =>
        WriteToElasticAsync(model, CancellationToken.None);

    public async Task<Result> WriteToElasticAsync(LogEntryDto model, CancellationToken cancellationToken = default)
    {
        try
        {
            model.Timestamp = DateTime.UtcNow;
            await _channel.Writer.WriteAsync(model, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk log queue yazımı sırasında hata oluştu.");
            return Result.Failure("Log kanalına yazılamadı.")
                .WithException(ex)
                .WithErrorCode(ErrorCode.DatabaseWriteFailed)
                .WithErrorType(ErrorType.Infrastructure)
                .WithStatusCode(StatusCodes.InternalServerError);
        }
    }

    public int PendingCount => _channel.Reader.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        var flushInterval = _opts.FlushInterval;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await reader.WaitToReadAsync(stoppingToken))
                    break;

                var batch = new List<LogEntryDto>(_opts.BatchSize);

                if (reader.TryRead(out var first))
                    batch.Add(first);

                while (batch.Count < _opts.BatchSize && reader.TryRead(out var next))
                    batch.Add(next);

                if (batch.Count > 0)
                {
                    await WriteToElasticSafeAsync(batch, stoppingToken);
                }

                await Task.Delay(flushInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }

        await DrainRemainingLogsAsync(reader, stoppingToken);
    }

    private async Task DrainRemainingLogsAsync(ChannelReader<LogEntryDto> reader, CancellationToken ct)
    {
        var remaining = new List<LogEntryDto>();
        while (reader.TryRead(out var item))
        {
            remaining.Add(item);
            if (remaining.Count >= _opts.BatchSize)
            {
                await WriteToElasticSafeAsync(remaining, ct);
                remaining.Clear();
            }
        }

        if (remaining.Count > 0)
        {
            await WriteToElasticSafeAsync(remaining, ct);
        }
    }

    private async Task WriteToElasticSafeAsync(List<LogEntryDto> batch, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var operations = new BulkOperationsCollection();
            foreach (var doc in batch)
            {
                operations.Add(new BulkIndexOperation<LogEntryDto>(doc));
            }

            var bulkReq = new BulkRequest(LogConstants.DataStreamName)
            {
                Operations = operations
            };

            var resp = await _client.BulkAsync(bulkReq, ct);

            if (resp.Errors)
            {
                var errs = resp.ItemsWithErrors
                    .Select(x => $"{x.Id}:{x.Error?.Reason}")
                    .ToArray();

                _logger.LogError("⛔ Bulk yazım hatası: {ErrorCount} entries. Details: {Errors}", errs.Length, string.Join("; ", errs));
            }
            else
            {
                _logger.LogDebug("✅ Bulk log yazımı başarılı. Count={Count}", batch.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Bulk log yazımı sırasında hata oluştu.");
        }
    }
}
