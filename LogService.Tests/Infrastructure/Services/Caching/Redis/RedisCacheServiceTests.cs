namespace LogService.Tests.Infrastructure.Services.Caching.Redis;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogService.Infrastructure.Services.Caching.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _service = new RedisCacheService(_cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_ValidJson_ShouldReturnDeserializedObject()
    {
        // Arrange
        string key = "test-key";
        var expected = new TestDto { Id = 1, Name = "Redis" };
        string json = JsonSerializer.Serialize(expected, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var bytes = Encoding.UTF8.GetBytes(json);

        _cacheMock.Setup(x => x.GetAsync(key, default))
                  .ReturnsAsync(bytes);

        // Act
        var result = await _service.GetAsync<TestDto>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Id, result!.Id);
        Assert.Equal(expected.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ShouldReturnDefault()
    {
        // Arrange
        string key = "missing-key";
        _cacheMock.Setup(x => x.GetAsync(key, default))
                  .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetAsync<TestDto>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenExceptionThrown_ShouldLogAndReturnDefault()
    {
        // Arrange
        string key = "error-key";
        _cacheMock.Setup(x => x.GetAsync(key, default))
                  .ThrowsAsync(new Exception("Redis is down"));

        // Act
        var result = await _service.GetAsync<TestDto>(key);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Cache’den okunurken hata")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_ValidObject_ShouldSerializeAndStore()
    {
        // Arrange
        string key = "test-key";
        var value = new TestDto { Id = 2, Name = "SetCache" };
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var bytes = Encoding.UTF8.GetBytes(json);

        DistributedCacheEntryOptions? capturedOptions = null;

        _cacheMock.Setup(x => x.SetAsync(key,
                                         It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
                                         It.IsAny<DistributedCacheEntryOptions>(),
                                         default))
                  .Callback<string, byte[], DistributedCacheEntryOptions, System.Threading.CancellationToken>((_, _, opt, _) => capturedOptions = opt)
                  .Returns(Task.CompletedTask)
                  .Verifiable();

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Assert
        _cacheMock.Verify();
        Assert.NotNull(capturedOptions);
        Assert.Equal(TimeSpan.FromMinutes(5), capturedOptions!.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task SetAsync_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        string key = "fail-key";
        var value = new TestDto { Id = 3, Name = "FailingCache" };

        _cacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default))
                  .ThrowsAsync(new Exception("Write failed"));

        // Act
        await _service.SetAsync(key, value, TimeSpan.FromMinutes(2));

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Cache’e yazılırken hata")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
