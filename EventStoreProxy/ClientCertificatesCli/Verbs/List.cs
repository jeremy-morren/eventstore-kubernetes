using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using CommandLine;

namespace ClientCertificatesCli.Verbs;

[Verb("list", HelpText = "List certificates")]
public class List : VerbBase
{
    public List(OutputFormat format, string? @namespace, string? secret)
        : base(@namespace, secret)
    {
        Format = format;
    }
    
    [Option('o', "output", Required = false, Default = OutputFormat.tsv, HelpText = "Output format. Supported formats: tsv, json")]
    public OutputFormat Format { get; }
    
    public void Handle(CertificatesService service)
    {
        var certificates = service.GetCertificates()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Name, c.Certificate.Thumbprint, User = User.Parse(c.Auth) });
        switch (Format)
        {
            case OutputFormat.json:
                var json = JsonSerializer.Serialize(certificates, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                Console.WriteLine(json);
                break;
            case OutputFormat.tsv:
                static void Write(string name, string username, string groups, string thumbprints)
                {
                    Console.WriteLine(
                        name.PadRight(20, ' ') + 
                        username.PadRight(20, ' ') +
                        groups.PadRight(20, ' ') +
                        thumbprints);
                }
                Write("NAME", "USERNAME", "GROUPS", "THUMBPRINT");
                foreach (var c in certificates)
                    Write(c.Name, c.User.Username, c.User.Groups ?? string.Empty, c.Thumbprint);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum OutputFormat
    {
        tsv, 
        json
    }

    private record User(string Username, string? Groups)
    {
        public static User Parse(string auth)
        {
            var index = auth.IndexOf(';');
            return new User(auth[..index].Trim(), auth[(index + 1)..].Trim());
        }
    }
}