namespace LogService.Infrastructure.HealthCheck.Methods.Security;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

[Name("rsa_key_provider_check")]
[HealthTags("security", "auth", "rsa", "jwt", "keys")]
public class RsaKeyProviderHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Secrets/public_key.pem");

            if (!File.Exists(fullPath))
                return Task.FromResult(HealthCheckResult.Unhealthy("Public key file not found."));

            var publicKeyText = File.ReadAllText(fullPath);

            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyText.ToCharArray());

            var rsaKey = new RsaSecurityKey(rsa);

            return rsaKey != null
                ? Task.FromResult(HealthCheckResult.Healthy("RSA public key loaded successfully."))
                : Task.FromResult(HealthCheckResult.Unhealthy("RSA key could not be created."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to load RSA key.", ex));
        }
    }
}
