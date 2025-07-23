using LogService.Domain.Constants;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Logging.Abstractions;
using LogService.Infrastructure.Services.Logging.Write;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Common.Results.Objects;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Logging.Write;

public class LogEntryWriteServiceTests
{
    private readonly Mock<IElasticClientAdapter> _adapterMock = new();
    private readonly Mock<ILogger<LogEntryWriteService>> _loggerMock = new();
    private readonly LogEntryWriteService _service;

    public LogEntryWriteServiceTests()
    {
        _service = new LogEntryWriteService(_adapterMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task WriteToElasticAsync_Should_Return_Success_When_Elastic_Response_Is_Valid()
    {
        var logEntry = CreateLogEntry();

        _adapterMock
            .Setup(adapter => adapter.IndexAsync(It.IsAny<global::Elastic.Clients.Elasticsearch.IndexRequest<LogEntryDto>>()))
            .ReturnsAsync(new FakeElasticResponse { IsValidResponse = true });

        var result = await _service.WriteToElasticAsync(logEntry);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task WriteToElasticAsync_Should_Return_Failure_When_Elastic_Response_Is_Invalid()
    {
        var logEntry = CreateLogEntry();

        _adapterMock
            .Setup(adapter => adapter.IndexAsync(It.IsAny<global::Elastic.Clients.Elasticsearch.IndexRequest<LogEntryDto>>()))
            .ReturnsAsync(new FakeElasticResponse
            {
                IsValidResponse = false,
                ErrorReason = "Indexing error"
            });

        var result = await _service.WriteToElasticAsync(logEntry);

        Assert.False(result.IsSuccess);
        Assert.Contains("Elastic log yazımı başarısız.", result.Errors);
        Assert.Equal(ErrorType.Infrastructure, result.ErrorType);
        Assert.Contains(ErrorCode.DatabaseWriteFailed, result.ErrorCodeEnums);
        Assert.Equal(StatusCodes.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task WriteToElasticAsync_Should_Return_Failure_On_Exception()
    {
        var logEntry = CreateLogEntry();

        _adapterMock
            .Setup(adapter => adapter.IndexAsync(It.IsAny<global::Elastic.Clients.Elasticsearch.IndexRequest<LogEntryDto>>()))
            .ThrowsAsync(new Exception("Simulated exception"));

        var result = await _service.WriteToElasticAsync(logEntry);

        Assert.False(result.IsSuccess);
        Assert.Contains("Elastic hatası (retry sonrası):", result.Errors[0]);
        Assert.Equal(ErrorType.Infrastructure, result.ErrorType);
        Assert.Contains(ErrorCode.DatabaseWriteFailed, result.ErrorCodeEnums);
        Assert.Equal(StatusCodes.InternalServerError, result.StatusCode);
        Assert.NotNull(result.Exception);
    }

    private static LogEntryDto CreateLogEntry() => new()
    {
        Message = "Test log",
        Level = ErrorLevel.Information,
        Timestamp = DateTime.UtcNow
    };

    private class FakeElasticResponse : IElasticResponseWrapper
    {
        public bool IsValidResponse { get; init; }
        public string? ErrorReason { get; init; }
    }
}
