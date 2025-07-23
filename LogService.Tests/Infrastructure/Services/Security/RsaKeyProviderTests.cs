using LogService.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;

namespace LogService.Tests.Infrastructure.Services.Security;
public class RsaKeyProviderTests
{
    private const string PublicKeyFileName = "Secrets/public_key.pem";

    private static string GenerateTestPublicKey()
    {
        using var rsa = RSA.Create(2048);
        return new string(PemEncoding.Write("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo()));
    }


    [Fact]
    public void Constructor_ShouldLoadRsaPublicKey_WhenFileExists()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RsaKeyProvider>>();
        var mockEnv = new Mock<IWebHostEnvironment>();

        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var fullPath = Path.Combine(contentRoot, PublicKeyFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var publicKeyPem = GenerateTestPublicKey();
        File.WriteAllText(fullPath, publicKeyPem);

        mockEnv.Setup(e => e.ContentRootPath).Returns(contentRoot);

        // Act
        var provider = new RsaKeyProvider(mockLogger.Object, mockEnv.Object);

        // Assert
        Assert.NotNull(provider.PublicKey);
        Assert.True(provider.PublicKey.Rsa is null); // Exported parameters, not direct RSA instance

        // Cleanup
        Directory.Delete(contentRoot, recursive: true);
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenFileDoesNotExist()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RsaKeyProvider>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        mockEnv.Setup(e => e.ContentRootPath).Returns(contentRoot);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RsaKeyProvider(mockLogger.Object, mockEnv.Object));

        Assert.Contains("RSA public key initialization failed", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldLogCritical_WhenInitializationFails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RsaKeyProvider>>();
        var envMock = new Mock<IWebHostEnvironment>();
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(contentRoot);
        var filePath = Path.Combine(contentRoot, PublicKeyFileName);

        // Invalid PEM content
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "not a valid key");

        envMock.Setup(e => e.ContentRootPath).Returns(contentRoot);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new RsaKeyProvider(loggerMock.Object, envMock.Object));

        // Assert
        loggerMock.Verify(l => l.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("❌ Failed to initialize RSA public key")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ), Times.Once);

        Directory.Delete(contentRoot, true);
    }
}