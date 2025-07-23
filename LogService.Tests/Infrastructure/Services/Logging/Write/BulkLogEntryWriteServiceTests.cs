using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using LogService.Application.Options;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Logging.Write;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using Xunit;
using Result = SharedKernel.Common.Results.Result;

namespace LogService.Tests.Infrastructure.Services.Logging.Write;

public class BulkLogEntryWriteServiceTests
{
    private readonly Mock<ElasticsearchClient> _elasticClientMock = new();
    private readonly Mock<ILogger<BulkLogEntryWriteService>> _loggerMock = new();
    private readonly IOptions<BulkLogOptions> _options = Options.Create(new BulkLogOptions
    {
        ChannelCapacity = 100,
        BatchSize = 10,
        FlushInterval = TimeSpan.FromMilliseconds(500)
    });

    private BulkLogEntryWriteService CreateService() =>
        new(_elasticClientMock.Object, _options, _loggerMock.Object);

    [Fact]
    public async Task WriteToElasticAsync_Should_Return_Success_When_Channel_Accepts()
    {
        // Arrange
        var service = CreateService();

        var dto = new LogEntryDto
        {
            Message = "Test log",
            Level = ErrorLevel.Information,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await service.WriteToElasticAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, service.PendingCount); // Channel'a yazıldığını kontrol et
    }

    [Fact]
    public async Task WriteToElasticAsync_Should_Return_Failure_When_Channel_IsFull()
    {
        // Arrange
        var service = new BulkLogEntryWriteService(_elasticClientMock.Object, _congestedOptions, _loggerMock.Object);

        var dto1 = new LogEntryDto
        {
            Message = "First log",
            Level = ErrorLevel.Information,
            Timestamp = DateTime.UtcNow
        };

        var dto2 = new LogEntryDto
        {
            Message = "Second log",
            Level = ErrorLevel.Information,
            Timestamp = DateTime.UtcNow
        };

        // İlk yazım başarılı olur
        var r1 = await service.WriteToElasticAsync(dto1);

        // Kanal hâlâ boşaltılmadığı için ikinci yazım TaskCanceledException fırlatırsa biz bunu bekliyoruz
        var cts = new CancellationTokenSource(50); // küçük timeout ile yapay hata üretelim

        var result = await service.WriteToElasticAsync(dto2, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Infrastructure, result.ErrorType);
        Assert.Equal(StatusCodes.InternalServerError, result.StatusCode);
        Assert.Contains(ErrorCode.DatabaseWriteFailed, result.ErrorCodeEnums);
        Assert.NotNull(result.Exception);
    }


    // Channel.Writer.WriteAsync() fırlatsın diye sahte servis
    private readonly IOptions<BulkLogOptions> _congestedOptions = Options.Create(new BulkLogOptions
    {
        ChannelCapacity = 1,
        BatchSize = 1,
        FlushInterval = TimeSpan.FromMilliseconds(500)
    });

}
