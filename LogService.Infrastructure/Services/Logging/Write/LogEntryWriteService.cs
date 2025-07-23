namespace LogService.Infrastructure.Services.Logging.Write;

using LogService.Domain.Constants;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.RetryPolicies;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Objects;

using Result = SharedKernel.Common.Results.Result;

public class LogEntryWriteService(IElasticClientAdapter elasticAdapter, ILogger<LogEntryWriteService> logger)
    : ILogEntryWriteService
{
    public async Task<Result> WriteToElasticAsync(LogEntryDto model)
    {
        try
        {
            var request = new global::Elastic.Clients.Elasticsearch.IndexRequest<LogEntryDto>(LogConstants.DataStreamName)
            {
                Document = model,
                OpType = global::Elastic.Clients.Elasticsearch.OpType.Create
            };

            var response = await PollyPolicies.RetryElasticPolicy.ExecuteAsync(() =>
                elasticAdapter.IndexAsync(request));


            if (!response.IsValidResponse)
            {
                var reason = response.ErrorReason ?? "Bilinmeyen hata";
                logger.LogWarning("❌ Elastic'e log yazılamadı. Reason: {Reason}", reason);

                return Result.Failure("Elastic log yazımı başarısız.", reason)
                    .WithErrorType(ErrorType.Infrastructure)
                    .WithErrorCode(ErrorCode.DatabaseWriteFailed)
                    .WithStatusCode(StatusCodes.InternalServerError);
            }

            logger.LogInformation("✅ Elastic log kaydı başarıyla yazıldı.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🔥 Elastic log yazımı sırasında beklenmeyen bir hata oluştu");

            return Result.Failure("Elastic hatası (retry sonrası): " + ex.Message)
                .WithException(ex)
                .WithErrorType(ErrorType.Infrastructure)
                .WithErrorCode(ErrorCode.DatabaseWriteFailed)
                .WithStatusCode(StatusCodes.InternalServerError);
        }
    }
}
