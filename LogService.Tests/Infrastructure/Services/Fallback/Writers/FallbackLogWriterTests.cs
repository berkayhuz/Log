using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Writers;
using LogService.Infrastructure.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using System.IO;
using System.Text.Json;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Fallback.Writers;

public class FallbackLogWriterTests : IDisposable
{
    private readonly string _testDir;
    private readonly FallbackLogWriter _writer;
    private readonly Mock<ILogEntryWriteService> _logWriterMock = new();
    private readonly Mock<ILogger<FallbackLogWriter>> _loggerMock = new();

    public FallbackLogWriterTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "FallbackLogsTest", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        _writer = new FallbackLogWriter(_logWriterMock.Object, _loggerMock.Object);

        // test directory override
        typeof(FallbackLogWriter)
            .GetField("_directoryPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_writer, _testDir);
    }

    [Fact]
    public async Task WriteAsync_Should_Create_Log_File()
    {
        var dto = CreateLogDto();

        await _writer.WriteAsync(dto);

        var files = Directory.GetFiles(_testDir, "*.json");
        Assert.Single(files);

        var json = await File.ReadAllTextAsync(files[0]);
        var readDto = JsonSerializer.Deserialize<LogEntryDto>(json);

        Assert.NotNull(readDto);
        Assert.Equal(dto.Message, readDto!.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Return_Valid_Dto()
    {
        var dto = CreateLogDto();
        var path = Path.Combine(_testDir, "test.json");
        var json = JsonSerializer.Serialize(dto);
        await File.WriteAllTextAsync(path, json);

        var result = await _writer.ReadAsync(path);

        Assert.NotNull(result);
        Assert.Equal(dto.Message, result!.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Return_Null_On_InvalidJson()
    {
        var path = Path.Combine(_testDir, "invalid.json");
        await File.WriteAllTextAsync(path, "not-a-json");

        var result = await _writer.ReadAsync(path);

        Assert.Null(result);
    }

    [Fact]
    public void Delete_Should_Remove_File()
    {
        var path = Path.Combine(_testDir, "delete-me.json");
        File.WriteAllText(path, "dummy");

        Assert.True(File.Exists(path));

        _writer.Delete(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task RetryPendingAsync_Should_Delete_File_On_Success()
    {
        var dto = CreateLogDto();
        var path = Path.Combine(_testDir, "retry.json");
        var json = JsonSerializer.Serialize(dto);
        await File.WriteAllTextAsync(path, json);

        _logWriterMock
            .Setup(w => w.WriteToElasticAsync(It.IsAny<LogEntryDto>()))
            .ReturnsAsync(Result.Success());

        await _writer.RetryPendingAsync();

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task RetryPendingAsync_Should_Keep_File_On_Failure()
    {
        var dto = CreateLogDto();
        var path = Path.Combine(_testDir, "retry-fail.json");
        var json = JsonSerializer.Serialize(dto);
        await File.WriteAllTextAsync(path, json);

        _logWriterMock
            .Setup(w => w.WriteToElasticAsync(It.IsAny<LogEntryDto>()))
            .ReturnsAsync(Result.Failure("fail"));

        await _writer.RetryPendingAsync();

        Assert.True(File.Exists(path));
    }

    private LogEntryDto CreateLogDto() => new()
    {
        Message = "test log",
        Level = ErrorLevel.Information,
        Timestamp = DateTime.UtcNow
    };

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
}
