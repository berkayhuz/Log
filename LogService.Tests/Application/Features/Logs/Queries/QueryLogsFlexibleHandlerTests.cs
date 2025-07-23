namespace LogService.Tests.Application.Features.Logs.Queries;

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Features.DTOs;
using LogService.Application.Features.Logs.Queries.QueryLogsFlexible;
using LogService.Application.Options;
using LogService.Domain.DTOs;
using Microsoft.AspNetCore.Http;
using Moq;
using SharedKernel.Common.Results;
using Xunit;

public class QueryLogsFlexibleHandlerTests
{
    private readonly Mock<ILogQueryService> _logQueryServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly QueryLogsFlexibleHandler _handler;

    public QueryLogsFlexibleHandlerTests()
    {
        _logQueryServiceMock = new Mock<ILogQueryService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _handler = new QueryLogsFlexibleHandler(
            _logQueryServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_UserHasRole_ShouldPassRoleToService()
    {
        // Arrange
        var role = "Admin";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role)
        }));

        var httpContext = new DefaultHttpContext
        {
            User = user
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new QueryLogsFlexible
        {
            IndexName = "my-index",
            Filter = new LogFilterDto
            {
                Page = 2,
                PageSize = 100,
                StartDate = System.DateTime.UtcNow.AddDays(-7),
                EndDate = System.DateTime.UtcNow,
            },
            Options = new FetchOptions
            {
                FetchCount = true,
                FetchDocuments = false,
                IncludeFields = new System.Collections.Generic.List<string> { "field1", "field2" }
            }
        };

        var expectedResult = Result<FlexibleLogQueryResult>.Success(new FlexibleLogQueryResult());

        _logQueryServiceMock.Setup(s => s.QueryLogsFlexibleAsync(
            request.IndexName,
            role,
            request.Filter,
            request.Options.FetchCount,
            request.Options.FetchDocuments,
            request.Options.IncludeFields))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result);
        _logQueryServiceMock.Verify(s => s.QueryLogsFlexibleAsync(
            request.IndexName,
            role,
            request.Filter,
            request.Options.FetchCount,
            request.Options.FetchDocuments,
            request.Options.IncludeFields), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRole_ShouldUseAnonymousRole()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // no role claim
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new QueryLogsFlexible
        {
            IndexName = "my-index",
            Filter = new LogFilterDto(),
            Options = new FetchOptions()
        };

        var expectedRole = "Anonymous";
        var expectedResult = Result<FlexibleLogQueryResult>.Success(new FlexibleLogQueryResult());

        _logQueryServiceMock.Setup(s => s.QueryLogsFlexibleAsync(
            request.IndexName,
            expectedRole,
            request.Filter,
            request.Options.FetchCount,
            request.Options.FetchDocuments,
            request.Options.IncludeFields))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result);
        _logQueryServiceMock.Verify(s => s.QueryLogsFlexibleAsync(
            request.IndexName,
            expectedRole,
            request.Filter,
            request.Options.FetchCount,
            request.Options.FetchDocuments,
            request.Options.IncludeFields), Times.Once);
    }

    [Fact]
    public void Request_ShouldGenerateCacheKey_AndExpiration()
    {
        // Arrange
        var request = new QueryLogsFlexible
        {
            IndexName = "logs-*",
            Filter = new LogFilterDto
            {
                Page = 1,
                PageSize = 20,
                StartDate = new System.DateTime(2024, 1, 1),
                EndDate = new System.DateTime(2024, 1, 5),
            }
        };

        // Act
        var cacheKey = request.CacheKey;
        var expiration = request.Expiration;

        // Assert
        Assert.NotNull(cacheKey);
        Assert.Contains("logs-*", cacheKey);
        Assert.Equal(System.TimeSpan.FromMinutes(3), expiration);
    }
}
