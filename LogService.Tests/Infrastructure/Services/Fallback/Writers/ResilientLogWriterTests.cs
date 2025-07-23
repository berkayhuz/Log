using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Fallback.Writers;
using LogService.Infrastructure.Services.Logging.Abstractions;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

namespace LogService.Tests.Infrastructure.Services.Fallback.Writers;

public class ResilientLogWriterTests
{
    private readonly Mock<ILogEntryWriteService> _innerWriterMock = new();
    private readonly Mock<IFallbackLogWriter> _fallbackWriterMock = new();
    private readonly ResilientLogWriter _service;

    public ResilientLogWriterTests()
    {
        _service = new ResilientLogWriter(_innerWriterMock.Object, _fallbackWriterMock.Object);
    }

    [Fact]
    public async Task WriteWithRetryAsync_Should_Return_Success_When_InnerWriter_Succeeds()
    {
        // Arrange
        var dto = CreateDto();

        _innerWriterMock
            .Setup(w => w.WriteToElasticAsync(dto))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _service.WriteWithRetryAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        _fallbackWriterMock.Verify(f => f.WriteAsync(It.IsAny<LogEntryDto>()), Times.Never);
    }

    [Fact]
    public async Task WriteWithRetryAsync_Should_Call_Fallback_When_InnerWriter_Fails()
    {
        // Arrange
        var dto = CreateDto();

        _innerWriterMock
            .Setup(w => w.WriteToElasticAsync(dto))
            .ReturnsAsync(Result.Failure("Elastic failed"));

        _fallbackWriterMock
            .Setup(f => f.WriteAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.WriteWithRetryAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        _fallbackWriterMock.Verify(f => f.WriteAsync(dto), Times.AtLeastOnce);
    }

    [Fact]
    public async Task WriteWithRetryAsync_Should_Handle_Exception_And_Trigger_Fallback()
    {
        // Arrange
        var dto = CreateDto();

        // Arrange
        _innerWriterMock
            .Setup(w => w.WriteToElasticAsync(dto))
            .ReturnsAsync(Result.Failure("fail")); // exception değil!

        _fallbackWriterMock
            .Setup(f => f.WriteAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.WriteWithRetryAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        _fallbackWriterMock.Verify(f => f.WriteAsync(dto), Times.AtLeastOnce);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Infrastructure, result.ErrorType);
        Assert.Equal(StatusCodes.ServiceUnavailable, result.StatusCode);
        _fallbackWriterMock.Verify(f => f.WriteAsync(dto), Times.AtLeastOnce);
    }

    private LogEntryDto CreateDto() => new()
    {
        Message = "test",
        Level = ErrorLevel.Information,
        Timestamp = DateTime.UtcNow
    };
}
