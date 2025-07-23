namespace LogService.Tests.Infrastructure.Services.Caching.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using LogService.Infrastructure.Services.Caching.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

public class StringDistributedCacheTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly StringDistributedCache _service;

    public StringDistributedCacheTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _service = new StringDistributedCache(_cacheMock.Object);
    }

    [Fact]
    public async Task GetStringAsync_ValidKey_ShouldCallCache()
    {
        // Arrange
        string key = "test-key";
        string expectedValue = "test-value";
        var bytes = System.Text.Encoding.UTF8.GetBytes(expectedValue);

        _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(bytes);

        // Act
        var result = await _service.GetStringAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
        _cacheMock.Verify(c => c.GetAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStringAsync_InvalidKey_ShouldReturnNull(string? key)
    {
        // Act
        var result = await _service.GetStringAsync(key!);

        // Assert
        Assert.Null(result);
        // 1) Cache'de var, basit string "hello" olarak saklanıyor
        _cacheMock.Setup(c => c.GetAsync("key1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("hello"));

        // 2) Cache'de yok, null dönüyor
        _cacheMock.Setup(c => c.GetAsync("missing-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);


    }

    [Fact]
    public async Task SetStringAsync_ValidArguments_ShouldCallCacheSet()
    {
        // Arrange
        string key = "key";
        string value = "value";
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);

        _cacheMock.Setup(c => c.SetAsync(
                            key,
                            It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == value),
                            It.IsAny<DistributedCacheEntryOptions>(),
                            It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask)
                  .Verifiable();

        // Act
        await _service.SetStringAsync(key, value);

        // Assert
        _cacheMock.Verify();
    }


    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData("key", null)]
    public async Task SetStringAsync_InvalidArguments_ShouldNotCallCacheSet(string? key, string? value)
    {
        // Act
        await _service.SetStringAsync(key!, value!);

        // Assert
        _cacheMock.Setup(c => c.GetAsync("key1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("hello"));

        // 2) Cache'de yok, null dönüyor
        _cacheMock.Setup(c => c.GetAsync("missing-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);
    }

    [Fact]
    public async Task RemoveAsync_ValidKey_ShouldCallCacheRemove()
    {
        // Arrange
        string key = "remove-key";

        _cacheMock.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveAsync(key);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveAsync_InvalidKey_ShouldNotCallCacheRemove(string? key)
    {
        // Act
        await _service.RemoveAsync(key!);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
