using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;

namespace EventStoreProxy.Authentication;

/// <summary>
/// Authenticates client using
/// certificates in <c>CertificateIssuerOptions.BasePath</c>
/// and associated thumbprint files containing <c>ES-TrustedAuth</c> header
/// </summary>
public class CertificateService
{
    private readonly ILogger<CertificateService> _logger;
    private readonly CertificatesWorker _certificatesWorker;

    public CertificateService(ILogger<CertificateService> logger, IEnumerable<IHostedService> hostedServices)
    {
        _logger = logger;
        _certificatesWorker = (CertificatesWorker)hostedServices.Single(s => s is CertificatesWorker);
    }

    public async Task OnCertificateValidated(CertificateValidatedContext context)
    {
        var certificates = await _certificatesWorker.GetClientCertificates();
        var cert = context.ClientCertificate;
        if (!certificates.TryGetValue(cert.Thumbprint, out var clientCert))
        {
            _logger.LogWarning("Certificate {@Certificate} not found, rejecting", GetCertificateInfo(cert));
            context.Fail($"Certificate with thumbprint {context.ClientCertificate.Thumbprint} not found");
            return;
        }
        var claims = new Claim[]
        {
            new (ClaimTypes.Name, clientCert.Name, ClaimValueTypes.String, context.Options.ClaimsIssuer),
            new (ClaimTypes.Role, clientCert.Auth, ClaimValueTypes.String, context.Options.ClaimsIssuer)
        };
        context.Principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, context.Scheme.Name));
        _logger.LogInformation("Authenticated certificate {@Certificate}", 
            new
            {
                clientCert.Name, 
                clientCert.Certificate.Thumbprint, 
                User = User.Parse(clientCert.Auth)
            });
        context.Success();
    }

    private static object GetCertificateInfo(X509Certificate2 cert)
    {
        return new
        {
            Name = cert.GetNameInfo(X509NameType.SimpleName, false),
            cert.Thumbprint
        };
    }

    public record User(string Username, string Groups)
    {
        public static User Parse(string auth)
        {
            var index = auth.IndexOf(';');
            //It can be -1 if no groups supplied
            return index == -1 
                ? new User(auth, string.Empty) 
                : new User(auth[..index], auth[(index + 1)..].Trim());
        }
    }
}