namespace LogService.Infrastructure.Services.Logging;

using System.Threading.Channels;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Clients.Elasticsearch.Core.Bulk;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.Application.Options;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Result = Application.Common.Results.Result;

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
        _channel = Channel.CreateUnbounded<LogEntryDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task<Result> WriteToElasticAsync(LogEntryDto model)
    {
        model.Timestamp = DateTime.UtcNow;
        _channel.Writer.TryWrite(model);
        return Task.FromResult(Result.Success());
    }

    public int PendingCount => _channel.Reader.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        var flushInterval = _opts.FlushInterval;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            var batch = new List<LogEntryDto>(_opts.BatchSize);
            batch.Add(await reader.ReadAsync(stoppingToken));
            while (batch.Count < _opts.BatchSize && reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            var operations = new BulkOperationsCollection();
            foreach (var doc in batch)
            {
                operations.Add(new BulkIndexOperation<LogEntryDto>(doc));
            }

            var bulkReq = new BulkRequest(LogConstants.DataStreamName)
            {
                Operations = operations
            };

            await TryCatch.ExecuteAsync(
                tryFunc: async () =>
                {
                    var resp = await _client.BulkAsync(bulkReq, stoppingToken);
                    if (resp.Errors)
                    {
                        var errs = resp.ItemsWithErrors
                            .Select(x => $"{x.Id}:{x.Error?.Reason}")
                            .ToArray();
                        _logger.LogError("Bulk yazım hatası: {Errors}", string.Join(';', errs));
                    }
                    else
                    {
                        _logger.LogDebug("Bulk başarıyla yazıldı: {Count}", batch.Count);
                    }
                },
                logger: _logger,
                context: nameof(ExecuteAsync)
            );

            await TryCatch.ExecuteAsync(
                tryFunc: () => Task.Delay(flushInterval, stoppingToken),
                logger: _logger,
                context: "BulkFlushDelay"
            );
        }
    }
}
