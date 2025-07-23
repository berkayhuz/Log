using LogService.Application.Abstractions.Requests;
using LogService.Application.Features.Logs.Cache;
using LogService.Domain.Constants;
using Moq;
using SharedKernel.Common.Results.Objects;

namespace LogService.Tests.Application.Features.Logs.ClearCache;

public class ClearCacheByRegionCommandHandlerTests
{
    private readonly Mock<ICacheRegionSupport> _cacheRegionSupportMock;
    private readonly ClearCacheByRegionCommandHandler _handler;

    public ClearCacheByRegionCommandHandlerTests()
    {
        _cacheRegionSupportMock = new Mock<ICacheRegionSupport>();
        _handler = new ClearCacheByRegionCommandHandler(_cacheRegionSupportMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallInvalidateRegionAndReturnSuccess()
    {
        // Arrange
        var indexName = "logs-index";
        var command = new ClearCacheByRegionCommand { IndexName = indexName };
        var expectedRegion = CacheConstants.RegionSetPrefix + indexName;

        _cacheRegionSupportMock
            .Setup(x => x.InvalidateRegionAsync(expectedRegion))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _cacheRegionSupportMock.Verify(x => x.InvalidateRegionAsync(expectedRegion), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidateThrowsException_ShouldReturnFailureWithMetadata()
    {
        // Arrange
        var indexName = "logs-error";
        var command = new ClearCacheByRegionCommand { IndexName = indexName };
        var expectedRegion = CacheConstants.RegionSetPrefix + indexName;
        var ex = new InvalidOperationException("Redis boom!");

        _cacheRegionSupportMock
            .Setup(x => x.InvalidateRegionAsync(expectedRegion))
            .ThrowsAsync(ex);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Cache temizleme sırasında hata oluştu.", result.Errors);
        Assert.Equal(ErrorType.Cache, result.ErrorType);
        Assert.Equal(ErrorCode.CacheUnavailable, result.ErrorCodeEnums[0]);
        Assert.Equal(StatusCodes.InternalServerError, result.StatusCode);
        Assert.Equal(ex.ToString(), result.Metadata?["Exception"]);
    }
}