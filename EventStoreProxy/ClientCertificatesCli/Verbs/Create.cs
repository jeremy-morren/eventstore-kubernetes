using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using Serilog;

namespace ClientCertificatesCli.Verbs;

[Verb("create", HelpText = "Creates a new client certificate")]
public class Create : VerbBase
{
    public Create(string name, int validity, string username, string groups, string? @namespace, string? secret)
        : base(@namespace, secret)
    {
        Name = name;
        Validity = validity;
        Username = username;
        Groups = groups;
    }

    [Option('n', "name", Required = true, HelpText = "Client certificate name")]
    public string Name { get; }

    [Option('l', "length", Required = false, Default = 60, HelpText = "Validity period in months")]
    public int Validity { get; }

    [Option('u', "username", Required = true, HelpText = "EventStore username")]
    public string Username { get; }
    
    [Option('g', "groups", Required = true, HelpText = "EventStore groups (comma separated)")]
    public string Groups { get; }

    public void Handle(CertificatesService service)
    {
        var certificates = service.GetCertificates().ToList();
        if (certificates.Any(c => c.Name == Name))
            throw new ArgumentException($"Certificate '{Name}' already exists");
        var cert = GenerateSelfSignedCertificate(Name, Validity);
        var auth = $"{Username}; {Groups}";
        certificates.Add(new ClientCertificate(Name, cert, auth));
        service.Save(certificates);
        Log.Information("Created certificate {@Certificate}", new { Name, cert.Thumbprint });
    }

    public static X509Certificate2 GenerateSelfSignedCertificate(string subject, int months, int keyStrength = 4096)
    {
        const string clientAuth = "1.3.6.1.5.5.7.3.2";
        var subj = new X500DistinguishedName($"CN={subject}");
        using var rsa = RSA.Create(keyStrength);
        var request = new CertificateRequest(subj, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(new OidCollection
            {
                Oid.FromOidValue(clientAuth, OidGroup.EnhancedKeyUsage)
            }, true));
        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddMonths(months));
    }
}