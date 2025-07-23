using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Elastic.Health;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Common.Results.Objects;

namespace LogService.Tests.Infrastructure.Services.Elastic.Health;

public class ElasticHealthServiceTests
{
    [Fact]
    public async Task IsElasticAvailableAsync_ShouldReturnSuccess_WhenPingIsValid()
    {
        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticHealthService>>();

        mockClient.Setup(x => x.PingAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new PingResult(true));

        var service = new ElasticHealthService(mockClient.Object, mockLogger.Object);
        var result = await service.IsElasticAvailableAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsElasticAvailableAsync_ShouldReturnFailure_WhenPingIsInvalid()
    {
        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticHealthService>>();

        mockClient.Setup(x => x.PingAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new PingResult(false));

        var service = new ElasticHealthService(mockClient.Object, mockLogger.Object);
        var result = await service.IsElasticAvailableAsync();

        Assert.True(result.IsFailure);
        Assert.Contains("Elasticsearch yanıtı geçersiz.", result.Errors);
    }


    [Fact]
    public async Task IsElasticAvailableAsync_ShouldReturnFailure_OnException()
    {
        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticHealthService>>();

        mockClient.Setup(x => x.PingAsync(It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("Simulated"));

        var service = new ElasticHealthService(mockClient.Object, mockLogger.Object);

        var result = await service.IsElasticAvailableAsync();

        Assert.True(result.IsFailure);
        Assert.Contains("istisna", result.Errors[0]);
        Assert.Equal(ErrorCode.ExternalServiceUnavailable, result.ErrorCodeEnums[0]);
    }
}
