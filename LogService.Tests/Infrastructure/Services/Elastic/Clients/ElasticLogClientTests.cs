using Elastic.Clients.Elasticsearch;
using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Elastic.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Elastic.Clients;

public class ElasticLogClientTests
{
    [Fact]
    public async Task QueryLogsFlexibleAsync_ReturnsSuccess_WhenResponseIsValid()
    {
        var filter = new LogFilterDto
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow
        };

        var allowedLevels = new List<ErrorLevel> { ErrorLevel.Information };

        var logEntry = new LogEntryDto
        {
            Level = ErrorLevel.Information,
            Message = "test log",
            Timestamp = DateTime.UtcNow
        };

        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticLogClient>>();

        mockClient.Setup(x => x.SearchAsync<LogEntryDto>(It.IsAny<SearchRequest<LogEntryDto>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new SearchResult<LogEntryDto>
            {
                IsValid = true,
                Documents = new List<LogEntryDto> { logEntry },
                TotalCount = 1
            });

        var client = new ElasticLogClient(mockClient.Object, mockLogger.Object);

        var result = await client.QueryLogsFlexibleAsync("logs-*", filter, allowedLevels, fetchCount: true, fetchDocuments: true);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Documents);
        Assert.Single(result.Value.Documents);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task QueryLogsFlexibleAsync_ReturnsFailure_WhenResponseInvalid()
    {
        var filter = new LogFilterDto
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow
        };

        var allowedLevels = new List<ErrorLevel> { ErrorLevel.Error };

        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticLogClient>>();

        mockClient.Setup(x => x.SearchAsync<LogEntryDto>(It.IsAny<SearchRequest<LogEntryDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult<LogEntryDto>
            {
                IsValid = false,
                ErrorReason = "Simulated error"
            });

        var client = new ElasticLogClient(mockClient.Object, mockLogger.Object);

        var result = await client.QueryLogsFlexibleAsync("logs-*", filter, allowedLevels);

        Assert.True(result.IsFailure);
        Assert.Contains("Elastic sorgusu başarısız", result.Errors[0]);
    }

    [Fact]
    public async Task QueryLogsFlexibleAsync_ReturnsFailure_OnException()
    {
        var filter = new LogFilterDto
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow
        };

        var allowedLevels = new List<ErrorLevel> { ErrorLevel.Critical };

        var mockClient = new Mock<IElasticClientWrapper>();
        var mockLogger = new Mock<ILogger<ElasticLogClient>>();

        mockClient.Setup(x => x.SearchAsync(It.IsAny<SearchRequest<LogEntryDto>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Boom"));

        var client = new ElasticLogClient(mockClient.Object, mockLogger.Object);

        var result = await client.QueryLogsFlexibleAsync("logs-*", filter, allowedLevels);

        Assert.True(result.IsFailure);
        Assert.Contains("Elastic sorgusu sırasında hata oluştu", result.Errors[0]);
        Assert.NotNull(result.Exception);
    }
}
