using CommandLine;
using Serilog;

namespace ClientCertificatesCli.Verbs;

[Verb("update", HelpText = "Updates an existing client certificate")]
public class Update : VerbBase
{
    public Update(string name, string username, string groups, string? @namespace, string? secret)
        : base(@namespace, secret)
    {
        Name = name;
        Username = username;
        Groups = groups;
    }

    [Option('n', "name", Required = true, HelpText = "Name of client certificate to update")]
    public string Name { get; }
    
    [Option('u', "username", Required = true, HelpText = "EventStore username")]
    public string Username { get; }
    
    [Option('g', Required = true, HelpText = "EventStore groups (comma separated)")]
    public string Groups { get; }

    public void Handle(CertificatesService service)
    {
        var certificates = service.GetCertificates().ToList();
        var cert = certificates.SingleOrDefault(c => c.Name == Name)
            ?? throw new ArgumentException($"Certificate '{Name}' not found");
        cert.Auth = $"{Username}; {Groups}";
        service.Save(certificates);
        Log.Information("Updated certificate {@Certificate}", 
            new { Name, cert.Certificate.Thumbprint });
    }
}