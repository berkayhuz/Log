namespace LogService.Infrastructure.Services.Elastic.Health;

using global::Elastic.Clients.Elasticsearch;

using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

using Result = SharedKernel.Common.Results.Result;

public class ElasticHealthService : IElasticHealthService
{
    private readonly IElasticClientWrapper _client;
    private readonly ILogger<ElasticHealthService> _logger;

    public ElasticHealthService(IElasticClientWrapper client, ILogger<ElasticHealthService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<bool>> IsElasticAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.PingAsync(cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("⛔ Elasticsearch yanıtı geçersiz.");
                return Result<bool>.Failure("Elasticsearch yanıtı geçersiz.")
                    .WithErrorCode(ErrorCode.ExternalServiceError)
                    .WithErrorType(ErrorType.DependencyFailure)
                    .WithStatusCode(StatusCodes.ServiceUnavailable);
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Elasticsearch sağlık kontrolü sırasında hata oluştu.");

            return Result<bool>.Failure("Elasticsearch sağlık kontrolü sırasında istisna oluştu.")
                .WithException(ex)
                .WithErrorType(ErrorType.DependencyFailure)
                .WithErrorCode(ErrorCode.ExternalServiceUnavailable)
                .WithStatusCode(StatusCodes.ServiceUnavailable);
        }
    }
}
