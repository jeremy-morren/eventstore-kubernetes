using CommandLine;

namespace ClientCertificatesCli.Verbs;

[Verb("delete", HelpText = "Deletes an existing certificate")]
public class Delete : VerbBase
{
    public Delete(string name, string? @namespace, string? secret)
        : base(@namespace, secret)
    {
        Name = name;
    }

    [Option('n', "name", Required = true, HelpText = "Name of client certificate to delete")]
    public string Name { get; }
    
    public void Handle(CertificatesService service)
    {
        var certificates = service.GetCertificates().ToList();
        var cert = certificates.SingleOrDefault(c => c.Name == Name)
                   ?? throw new ArgumentException($"Certificate '{Name}' not found");
        certificates.Remove(cert);
        service.Save(certificates);
    }
}