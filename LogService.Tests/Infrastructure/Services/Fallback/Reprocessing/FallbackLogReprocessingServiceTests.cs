using LogService.Application.Options;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Fallback.Reprocessing;
using LogService.Infrastructure.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Fallback.Reprocessing;

public class FallbackLogReprocessingServiceTests
{
    private readonly Mock<IFallbackLogWriter> _fallbackWriter = new();
    private readonly Mock<IElasticHealthService> _elasticHealthService = new();
    private readonly Mock<IResilientLogWriter> _resilientWriter = new();
    private readonly Mock<ILogEntryWriteService> _directWriter = new();
    private readonly Mock<IOptionsMonitor<FallbackProcessingRuntimeOptions>> _opts = new();
    private readonly Mock<ILogger<FallbackLogReprocessingService>> _logger = new();

    [Fact]
    public async Task Should_Skip_File_When_Elastic_Is_Down()
    {
        // Arrange
        var options = new FallbackProcessingRuntimeOptions { EnableResilient = false };
        _opts.Setup(x => x.CurrentValue).Returns(options);

        _elasticHealthService.Setup(x => x.IsElasticAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        var service = new FallbackLogReprocessingService(
            _fallbackWriter.Object,
            _elasticHealthService.Object,
            _resilientWriter.Object,
            _directWriter.Object,
            _opts.Object,
            _logger.Object
        );

        var cts = new CancellationTokenSource();
        var testFilePath = Path.GetTempFileName();
        File.WriteAllText(testFilePath, "{}");

        var channel = typeof(FallbackLogReprocessingService)
            .GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(service) as Channel<string>;

        await channel!.Writer.WriteAsync(testFilePath, cts.Token);

        var task = service.StartAsync(cts.Token);
        await Task.Delay(300); // wait briefly
        cts.Cancel();

        // Assert
        _fallbackWriter.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Process_Valid_File_And_Delete_When_Success()
    {
        // Arrange
        var dto = new LogEntryDto
        {
            Level = ErrorLevel.Information,
            Message = "Dummy log",
            Timestamp = DateTime.UtcNow
        };

        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "{}");

        var options = new FallbackProcessingRuntimeOptions { EnableResilient = false };
        _opts.Setup(x => x.CurrentValue).Returns(options);
        _elasticHealthService.Setup(x => x.IsElasticAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _fallbackWriter.Setup(x => x.ReadAsync(It.IsAny<string>())).ReturnsAsync(dto);
        _directWriter.Setup(x => x.WriteToElasticAsync(dto)).ReturnsAsync(Result.Success());

        var service = new FallbackLogReprocessingService(
            _fallbackWriter.Object,
            _elasticHealthService.Object,
            _resilientWriter.Object,
            _directWriter.Object,
            _opts.Object,
            _logger.Object
        );

        var channel = typeof(FallbackLogReprocessingService)
            .GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(service) as Channel<string>;

        await channel!.Writer.WriteAsync(filePath);

        var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        await Task.Delay(300); // allow time for processing
        cts.Cancel();

        // Assert
        _fallbackWriter.Verify(x => x.Delete(filePath), Times.Once);
        _directWriter.Verify(x => x.WriteToElasticAsync(dto), Times.Once);
    }
}
