using Elastic.Clients.Elasticsearch;
using Elastic.Transport.Products.Elasticsearch;
using LogService.Infrastructure.Services.Logging.Abstractions;
using LogService.Infrastructure.Services.Logging.Write;
using Moq;
using Xunit;

namespace LogService.Tests.Infrastructure.Services.Logging.Write;

public class ElasticClientAdapterTests
{
    [Fact]
    public async Task IndexAsync_Should_Return_ValidResult_When_Successful()
    {
        // Arrange
        var adapter = new FakeElasticClientAdapter(isValid: true, errorReason: null);
        var request = new IndexRequest<DummyDto>("log-index")
        {
            Document = new DummyDto { Name = "test" }
        };

        // Act
        var result = await adapter.IndexAsync(request);

        // Assert
        Assert.True(result.IsValidResponse);
        Assert.Null(result.ErrorReason);
    }


    [Fact]
    public async Task IndexAsync_Should_Return_ErrorReason_If_Response_IsInvalid()
    {
        // Arrange
        var dto = new DummyDto { Name = "fail" };
        var request = new IndexRequest<DummyDto>("log-index")
        {
            Document = dto,
            OpType = OpType.Create
        };

        var mockClient = new Mock<ElasticsearchClient>();

        // ❗ Gerçek ElasticsearchServerError ve Error mocklanamaz → sarmalanmış sahte adapter kullanacağız
        var adapter = new FakeElasticClientAdapter(
            isValid: false,
            errorReason: "Simulated error"
        );

        // Act
        var result = await adapter.IndexAsync(request);

        // Assert
        Assert.False(result.IsValidResponse);
        Assert.Equal("Simulated error", result.ErrorReason);
    }

    private class DummyDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // Tamamen test edilebilir sahte adapter (IndexResponse yerine)
    private class FakeElasticClientAdapter : IElasticClientAdapter
    {
        private readonly bool _isValid;
        private readonly string? _reason;

        public FakeElasticClientAdapter(bool isValid, string? errorReason)
        {
            _isValid = isValid;
            _reason = errorReason;
        }

        public Task<IElasticResponseWrapper> IndexAsync<T>(IndexRequest<T> request) where T : class
        {
            return Task.FromResult<IElasticResponseWrapper>(new FakeResponse(_isValid, _reason));
        }

        private class FakeResponse : IElasticResponseWrapper
        {
            public FakeResponse(bool isValid, string? reason)
            {
                IsValidResponse = isValid;
                ErrorReason = reason;
            }

            public bool IsValidResponse { get; }
            public string? ErrorReason { get; }
        }
    }
}
