namespace LogService.Infrastructure.Services.Security;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

public class RsaKeyProvider
{
    public RsaSecurityKey PublicKey { get; }
    private const string PublicKeyPath = "Secrets/public_key.pem";

    public RsaKeyProvider()
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), PublicKeyPath);
        var publicKeyText = File.ReadAllText(fullPath);

        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyText.ToCharArray());

        PublicKey = new RsaSecurityKey(rsa);
    }
}

