using LogService.Application.Features.DTOs;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Logging.Read;
using Moq;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Logging.Read;

public class LogQueryServiceTests
{
    private readonly Mock<IElasticLogClient> _elasticClientMock = new();
    private readonly LogQueryService _service;

    public LogQueryServiceTests()
    {
        _service = new LogQueryService(_elasticClientMock.Object);
    }

    [Fact]
    public async Task QueryLogsFlexibleAsync_Should_Return_Forbidden_When_NoAccess()
    {
        // Arrange
        var filter = new LogFilterDto();
        var role = "no-access-role"; // RoleLogStageMap içinde olmayan rol

        // Act
        var result = await _service.QueryLogsFlexibleAsync(
            indexName: "logs-index",
            role: role,
            filter: filter,
            fetchCount: true,
            fetchDocuments: true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Forbidden, result.StatusCode);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Contains(ErrorCode.AccessDenied, result.ErrorCodeEnums);
    }

    [Fact]
    public async Task QueryLogsFlexibleAsync_Should_Delegate_To_ElasticClient_When_AccessGranted()
    {
        // Arrange
        var indexName = "logs-index";
        var role = "admin";
        var filter = new LogFilterDto();
        var fetchCount = true;
        var fetchDocuments = true;
        var allowedLevels = new List<ErrorLevel> { ErrorLevel.Information };
        List<string>? includeFields = null;
        var expected = Result<FlexibleLogQueryResult>.Success(new());

        _elasticClientMock
            .Setup(x => x.QueryLogsFlexibleAsync(
                indexName,
                filter,
                It.IsAny<List<ErrorLevel>>(), // çünkü içeride dönüş var
                fetchCount,
                fetchDocuments,
                includeFields,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.QueryLogsFlexibleAsync(
            indexName,
            role,
            filter,
            fetchCount,
            fetchDocuments);

        // Assert
        Assert.True(result.IsSuccess);
    }




    [Fact]
    public async Task QueryLogsFlexibleAsync_Should_Return_Failure_When_Exception_Thrown()
    {
        // Arrange
        var role = "admin";
        var filter = new LogFilterDto();
        var indexName = "logs-index";
        var fetchCount = true;
        var fetchDocuments = false;
        var allowedLevels = new List<ErrorLevel> { ErrorLevel.Information };
        List<string>? includeFields = null;

        _elasticClientMock
            .Setup(x => x.QueryLogsFlexibleAsync(
                It.IsAny<string>(),
                It.IsAny<LogFilterDto>(),
                It.IsAny<List<ErrorLevel>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<List<string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act
        var result = await _service.QueryLogsFlexibleAsync(
            indexName: indexName,
            role: role,
            filter: filter,
            fetchCount: fetchCount,
            fetchDocuments: fetchDocuments);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.InternalServerError, result.StatusCode);
        Assert.Equal(ErrorType.Unexpected, result.ErrorType);
        Assert.NotNull(result.Exception);
    }

}
