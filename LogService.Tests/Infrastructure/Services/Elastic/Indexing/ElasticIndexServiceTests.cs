using LogService.Infrastructure.Services.Elastic.Indexing;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace LogService.Tests.Infrastructure.Services.Elastic.Indexing;

public class ElasticIndexServiceTests
{
    private readonly Mock<ILogger<ElasticIndexService>> _logger = new();

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("http://localhost:9200")
        };
    }

    [Fact]
    public async Task GetIndexNamesAsync_ShouldReturnIndexList_WhenResponseIsSuccessful()
    {
        // Arrange
        var json = """
        [
            { "index": "logs-2025-01" },
            { "index": "logs-2025-02" },
            { "index": "logs-2025-01" },
            { "index": "" }
        ]
        """;

        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var service = new ElasticIndexService(httpClient, _logger.Object);

        // Act
        var result = await service.GetIndexNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("logs-2025-01", result);
        Assert.Contains("logs-2025-02", result);
    }

    [Fact]
    public async Task GetIndexNamesAsync_ShouldReturnEmptyList_OnHttpFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "");
        var service = new ElasticIndexService(httpClient, _logger.Object);

        // Act
        var result = await service.GetIndexNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetIndexNamesAsync_ShouldReturnEmptyList_OnDeserializationError()
    {
        // Arrange
        var invalidJson = """ { "not": "a valid array" } """;
        var httpClient = CreateHttpClient(HttpStatusCode.OK, invalidJson);
        var service = new ElasticIndexService(httpClient, _logger.Object);

        // Act
        var result = await service.GetIndexNamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
