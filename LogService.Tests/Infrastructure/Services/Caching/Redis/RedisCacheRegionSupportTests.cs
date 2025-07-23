namespace LogService.Tests.Infrastructure.Services.Caching.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;
using LogService.Infrastructure.Services.Caching.Redis;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

public class RedisCacheRegionSupportTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<ILogger<RedisCacheRegionSupport>> _loggerMock;
    private readonly RedisCacheRegionSupport _service;

    public RedisCacheRegionSupportTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisCacheRegionSupport>>();

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                  .Returns(_dbMock.Object);

        _service = new RedisCacheRegionSupport(_redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddKeyToRegionAsync_ValidInput_ShouldAddKey()
    {
        // Arrange
        string region = "logs";
        string key = "log:123";
        var expectedKey = (RedisKey)("cache:region:" + region); // kritik düzeltme!

        _dbMock.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
               .ReturnsAsync(true);

        // Act
        await _service.AddKeyToRegionAsync(region, key);

        // Assert
        _dbMock.Verify(x =>
            x.SetAddAsync(expectedKey, key, CommandFlags.None), Times.Once);
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("region", null)]
    [InlineData("", "key")]
    [InlineData("region", "")]
    public async Task AddKeyToRegionAsync_InvalidInput_ShouldNotThrow(string region, string key)
    {
        // Act
        var exception = await Record.ExceptionAsync(() =>
            _service.AddKeyToRegionAsync(region, key));

        // Assert
        Assert.Null(exception);
        _dbMock.Verify(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateRegionAsync_ValidRegion_ShouldDeleteKeysAndRegionKey()
    {
        // Arrange
        string region = "auth";
        var regionKey = (RedisKey)("cache:region:" + region); // ✔️ Doğru prefix burada!
        RedisValue[] keys = { "key1", "key2" };

        _dbMock.Setup(x => x.SetMembersAsync(It.IsAny<RedisKey>(), CommandFlags.None))
               .ReturnsAsync(keys);

        _dbMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), CommandFlags.None))
               .ReturnsAsync(2);

        _dbMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None))
               .ReturnsAsync(true);

        // Act
        await _service.InvalidateRegionAsync(region);

        // Assert
        _dbMock.Verify(x =>
            x.SetMembersAsync(regionKey, CommandFlags.None), Times.Once);

        _dbMock.Verify(x =>
            x.KeyDeleteAsync(
                It.Is<RedisKey[]>(r => r.SequenceEqual(keys.Select(k => (RedisKey)(string)k))),
                CommandFlags.None), Times.Once);

        _dbMock.Verify(x =>
            x.KeyDeleteAsync(regionKey, CommandFlags.None), Times.Once);
    }




    [Fact]
    public async Task InvalidateRegionAsync_EmptyOrNull_ShouldNotThrow()
    {
        await _service.InvalidateRegionAsync(null);
        await _service.InvalidateRegionAsync(string.Empty);

        _dbMock.Verify(x => x.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }
}

