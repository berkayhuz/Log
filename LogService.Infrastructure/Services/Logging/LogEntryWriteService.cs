namespace LogService.Infrastructure.Services.Logging;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Transport;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Abstractions.Requests;
using LogService.Infrastructure.Services.Fallback;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

using Result = Application.Common.Results.Result;

public class LogEntryWriteService(
    ElasticsearchClient elasticClient,
    ILogger<LogEntryWriteService> logger) : ILogEntryWriteService
{
    public async Task<Result> WriteToElasticAsync(LogEntryDto model)
    {
        model.Timestamp = DateTime.UtcNow;

        return await TryCatch.ExecuteAsync(
            tryFunc: async () =>
            {
                var indexRequest = new IndexRequest<LogEntryDto>(LogConstants.DataStreamName)
                {
                    Document = model,
                    OpType = OpType.Create,
                    RequestConfiguration = new RequestConfiguration
                    {
                        ContentType = "application/json",
                        Accept = "application/json"
                    }
                };

                var result = await PollyPolicies.RetryElasticPolicy.ExecuteAsync(() =>
                    elasticClient.IndexAsync(indexRequest));

                return result.IsValidResponse
                    ? Result.Success()
                    : Result.Failure("Log yazılamadı.", result.ElasticsearchServerError?.Error?.Reason);
            },
            catchFunc: ex => Task.FromResult(Result.Failure($"Elastic hatası (retry sonrası): {ex.Message}")),
            logger: logger,
            context: "LogEntryWriteService.WriteToElasticAsync"
        );
    }
}
