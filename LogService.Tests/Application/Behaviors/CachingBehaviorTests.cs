using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Behaviors.Pipeline;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LogService.Tests.Application.Behaviors;

public class CachingBehaviorTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var request = new FakeRequest();
        var cachedResult = Result<string>.Success("cached value");

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<Result<string>>(request.CacheKey))
                 .ReturnsAsync(cachedResult);

        var regionMock = new Mock<ICacheRegionSupport>();
        var loggerMock = new Mock<ILogger<CachingBehavior<FakeRequest, Result<string>>>>();

        var behavior = new CachingBehavior<FakeRequest, Result<string>>(cacheMock.Object, regionMock.Object, loggerMock.Object);

        RequestHandlerDelegate<Result<string>> next = delegate
        {
            throw new Exception("Should not call handler");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("cached value", result.Value);
        cacheMock.Verify(c => c.GetAsync<Result<string>>(request.CacheKey), Times.Once);
        cacheMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_CacheMiss_CallsNextAndSetsCache()
    {
        // Arrange
        var request = new FakeRequest();
        var resultFromHandler = Result<string>.Success("handler value");

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<Result<string>>(request.CacheKey))
                 .ReturnsAsync((Result<string>?)null);

        var regionMock = new Mock<ICacheRegionSupport>();
        var loggerMock = new Mock<ILogger<CachingBehavior<FakeRequest, Result<string>>>>();

        var behavior = new CachingBehavior<FakeRequest, Result<string>>(cacheMock.Object, regionMock.Object, loggerMock.Object);

        RequestHandlerDelegate<Result<string>> next = delegate
        {
            return Task.FromResult(resultFromHandler);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("handler value", result.Value);
        cacheMock.Verify(c => c.SetAsync(request.CacheKey, resultFromHandler, request.Expiration!.Value), Times.Once);
        regionMock.Verify(r => r.AddKeyToRegionAsync(request.CacheRegion!, request.CacheKey), Times.Once);
    }

    [Fact]
    public async Task Handle_CacheSetThrows_ExceptionHandled_AndContinues()
    {
        // Arrange
        var request = new FakeRequest();
        var handlerResult = Result<string>.Success("val");

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<Result<string>>(request.CacheKey))
                 .ReturnsAsync((Result<string>?)null);

        cacheMock.Setup(c => c.SetAsync(request.CacheKey, handlerResult, request.Expiration!.Value))
                 .ThrowsAsync(new InvalidOperationException("redis kapandı"));

        var regionMock = new Mock<ICacheRegionSupport>();
        var loggerMock = new Mock<ILogger<CachingBehavior<FakeRequest, Result<string>>>>();

        var behavior = new CachingBehavior<FakeRequest, Result<string>>(cacheMock.Object, regionMock.Object, loggerMock.Object);

        RequestHandlerDelegate<Result<string>> next = delegate
        {
            return Task.FromResult(handlerResult);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("val", result.Value);
    }

    [Fact]
    public async Task Handle_BypassCache_SkipsCacheCompletely()
    {
        // Arrange
        var request = new FakeBypassRequest();
        var handlerResult = Result<string>.Success("bypass");

        var cacheMock = new Mock<ICacheService>();
        var regionMock = new Mock<ICacheRegionSupport>();
        var loggerMock = new Mock<ILogger<CachingBehavior<FakeBypassRequest, Result<string>>>>();

        var behavior = new CachingBehavior<FakeBypassRequest, Result<string>>(cacheMock.Object, regionMock.Object, loggerMock.Object);

        RequestHandlerDelegate<Result<string>> next = delegate
        {
            return Task.FromResult(handlerResult);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("bypass", result.Value);
        cacheMock.Verify(c => c.GetAsync<Result<string>>(It.IsAny<string>()), Times.Never);
        cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Result<string>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    // Mock Request Types
    public class FakeRequest : ICachableRequest
    {
        public string CacheKey => "key:fake";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
        public string? CacheRegion => "region:fake";
    }

    public class FakeBypassRequest : ICachableRequest, ICacheBypassableRequest
    {
        public string CacheKey => "key:bypass";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
        public string? CacheRegion => "region:bypass";
        public bool BypassCache => true;
    }
}
