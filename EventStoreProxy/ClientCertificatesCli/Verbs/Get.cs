using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using Serilog;

namespace ClientCertificatesCli.Verbs;

[Verb("get", HelpText = "Gets a certificate in base64 encoded PKCS12 format")]
public class Get : VerbBase
{
    public Get(string name, string password, string? @namespace, string? secret)
        : base(@namespace, secret)
    {
        Name = name;
        Password = password;
    }

    [Option('n', "name", Required = true, HelpText = "Client certificate name")]
    public string Name { get; }
    
    [Option("password", Required = false, HelpText = "Certificate password")]
    public string? Password { get; }

    public void Handle(CertificatesService service)
    {
        var certificates = service.GetCertificates().ToList();
        var cert = certificates.SingleOrDefault(c => c.Name == Name)
                   ?? throw new ArgumentException($"Certificate '{Name}' not found");
        var bytes = cert.Certificate.Export(X509ContentType.Pkcs12, Password);
        Console.WriteLine(Convert.ToBase64String(bytes));
    }
}