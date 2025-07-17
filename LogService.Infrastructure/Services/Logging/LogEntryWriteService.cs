namespace LogService.Infrastructure.Services.Logging;

using global::Elastic.Clients.Elasticsearch;
using global::Elastic.Transport;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Abstractions.Requests;
using LogService.Infrastructure.Services.Fallback;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.DTOs;

using Result = Application.Common.Results.Result;

public class LogEntryWriteService(
    ElasticsearchClient elasticClient,
    ICacheRegionSupport regionSupport) : ILogEntryWriteService
{
    public async Task<Result> WriteToElasticAsync(LogEntryDto model)
    {
        const string className = nameof(LogEntryWriteService);

        model.Timestamp = DateTime.UtcNow;

        try
        {
            var indexRequest = new IndexRequest<LogEntryDto>(LogConstants.DataStreamName)
            {
                Document = model,
                OpType = OpType.Create
            };

            indexRequest.RequestConfiguration = new RequestConfiguration
            {
                ContentType = "application/json",
                Accept = "application/json"
            };

            var result = await PollyPolicies.RetryElasticPolicy.ExecuteAsync(() =>
            {
                return elasticClient.IndexAsync(indexRequest);
            });


            if (result.IsValidResponse)
            {
                return Result.Success();
            }
            else
            {
                var reason = result.ElasticsearchServerError?.Error?.Reason;
                return Result.Failure("Log yazılamadı.", reason);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Elastic hatası (retry sonrası): {ex.Message}");
        }
    }
}
