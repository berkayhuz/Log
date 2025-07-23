using LogService.Application.Options;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Logging.Write;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using System.Text;
using System.Text.Json;

namespace LogService.Tests.Infrastructure.Services.Logging.Write;

public class LogConsumerServiceTests
{
    private readonly Mock<IResilientLogWriter> _resilientWriterMock = new();
    private readonly Mock<ILogger<LogConsumerService>> _loggerMock = new();

    private LogConsumerService _service => new(_resilientWriterMock.Object, OptionsMock(), _loggerMock.Object);

    [Fact]
    public async Task ProcessMessageAsync_Should_Return_Success_When_Valid_Log_Is_Processed()
    {
        // Arrange
        var dto = new LogEntryDto
        {
            Message = "test",
            Level = ErrorLevel.Information,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(dto);
        var args = BuildArgs(json);

        _resilientWriterMock
            .Setup(w => w.WriteWithRetryAsync(It.IsAny<LogEntryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());


        // Act
        var result = await CallPrivateProcessMessageAsync(args);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ProcessMessageAsync_Should_Return_Failure_When_Json_Is_Invalid()
    {
        var json = "{ invalid json";
        var args = BuildArgs(json);

        var result = await CallPrivateProcessMessageAsync(args);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Serialization, result.ErrorType);
        Assert.Equal(false, result.Metadata?["Requeue"]);
    }

    [Fact]
    public async Task ProcessMessageAsync_Should_Return_Failure_When_Deserialization_Is_Null()
    {
        var json = "null"; // JsonSerializer.Deserialize returns null
        var args = BuildArgs(json);

        var result = await CallPrivateProcessMessageAsync(args);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Serialization, result.ErrorType);
        Assert.Equal(false, result.Metadata?["Requeue"]);
    }

    [Fact]
    public async Task ProcessMessageAsync_Should_Return_Failure_When_Write_Fails()
    {
        var dto = new LogEntryDto
        {
            Message = "fail write",
            Level = ErrorLevel.Error,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(dto);
        var args = BuildArgs(json);

        _resilientWriterMock
            .Setup(w => w.WriteWithRetryAsync(It.IsAny<LogEntryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await CallPrivateProcessMessageAsync(args);

        Assert.True(result.IsFailure);
        Assert.Equal(true, result.Metadata?["Requeue"]);
    }

    [Fact]
    public async Task ProcessMessageAsync_Should_Return_Failure_On_Exception()
    {
        var dto = new LogEntryDto
        {
            Message = "throws",
            Level = ErrorLevel.Critical,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(dto);
        var args = BuildArgs(json);

        _resilientWriterMock
            .Setup(w => w.WriteWithRetryAsync(It.IsAny<LogEntryDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await CallPrivateProcessMessageAsync(args);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unexpected, result.ErrorType);
        Assert.Equal(true, result.Metadata?["Requeue"]);
    }

    private static BasicDeliverEventArgs BuildArgs(string json)
    {
        var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(json));

        var propertiesMock = new Mock<IReadOnlyBasicProperties>();

        return new BasicDeliverEventArgs(
            consumerTag: "test-consumer",
            deliveryTag: 42,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing-key",
            properties: propertiesMock.Object,
            body: body,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Result> CallPrivateProcessMessageAsync(BasicDeliverEventArgs args)
    {
        var service = _service;

        // Use reflection to call private method
        var method = typeof(LogConsumerService).GetMethod("ProcessMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var task = (Task<Result>)method.Invoke(service, new object[] { args })!;
        return await task;
    }

    private static Microsoft.Extensions.Options.IOptions<RabbitMqSettings> OptionsMock()
    {
        return Microsoft.Extensions.Options.Options.Create(new RabbitMqSettings
        {
            Host = "localhost",
            Port = 5672,
            Username = "guest",
            Password = "guest",
            LogQueueName = "log.test.queue"
        });
    }
}
