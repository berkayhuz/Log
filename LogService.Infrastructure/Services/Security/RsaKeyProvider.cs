namespace LogService.Infrastructure.Services.Security;

using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public sealed class RsaKeyProvider
{
    public RsaSecurityKey PublicKey { get; }

    private const string PublicKeyPath = "Secrets/public_key.pem";

    public RsaKeyProvider(ILogger<RsaKeyProvider> logger, IWebHostEnvironment env)
    {
        try
        {
            var fullPath = Path.Combine(env.ContentRootPath, PublicKeyPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("RSA public key file not found", fullPath);

            var publicKeyText = File.ReadAllText(fullPath);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyText);

            PublicKey = new RsaSecurityKey(rsa.ExportParameters(false));

            logger.LogInformation("✅ RSA public key successfully loaded from {Path}", fullPath);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "❌ Failed to initialize RSA public key");
            throw new InvalidOperationException("RSA public key initialization failed.", ex);
        }
    }
}
