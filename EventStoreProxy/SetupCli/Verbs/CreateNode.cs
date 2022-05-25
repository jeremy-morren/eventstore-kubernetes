using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommandLine;

namespace EventStore.Setup.Verbs;

[Verb("create-node", HelpText = "Creates a certificate for a node")]
public class CreateNode
{
    public CreateNode(string caCert,
        string caKey,
        IEnumerable<string> dnsNames,
        IEnumerable<string> ipAddresses,
        string @out,
        int length)
    {
        CaCert = caCert;
        CaKey = caKey;
        DnsNames = dnsNames;
        IpAddresses = ipAddresses;
        Out = @out;
        Length = length;
    }

    [Option("ca-cert", Required = true, HelpText = "CA Certificates")]
    public string CaCert { get; }

    [Option("ca-key", Required = true, HelpText = "CA Certificate private key")]
    public string CaKey { get; }

    [Option("dns-names", Required = false, HelpText = "DNS names (comma separated)", Separator = ',')]
    public IEnumerable<string> DnsNames { get; }

    [Option("ip-addresses", Required = false, HelpText = "IP Addresses (comma separated)", Separator = ',')]
    public IEnumerable<string> IpAddresses { get; }

    [Option("out", Required = true, HelpText = "Output directory (will create node.crt and node.key)")]
    public string Out { get; }

    [Option('l', "length", Required = false, Default = 12, HelpText = "Validity in months")]
    public int Length { get; }

    public void Process()
    {
        var ca = X509Certificate2.CreateFromPemFile(CaCert, CaKey);
        var cert = CreateSignedCertificate(ca, "eventstoredb-node", Length, DnsNames, IpAddresses);
        ExportCert(cert, Out, "node");
    }

    private static X509Certificate2 CreateSignedCertificate(X509Certificate2 caCert, 
        string subject,
        int months, 
        IEnumerable<string> dnsNames,
        IEnumerable<string> ipAddresses,
        int keyStrength = 4096)
    {
        const string clientAuth = "1.3.6.1.5.5.7.3.2";
        const string serverAuth = "1.3.6.1.5.5.7.3.1";
        var subj = new X500DistinguishedName($"CN={subject}");
        using var rsa = RSA.Create(keyStrength);
        var request = new CertificateRequest(subj, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
        {
            CertificateExtensions =
            {
                new X509BasicConstraintsExtension(false, false, 0, true),
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true),
                new X509EnhancedKeyUsageExtension(new OidCollection()
                {
                    Oid.FromOidValue(clientAuth, OidGroup.EnhancedKeyUsage),
                    Oid.FromOidValue(serverAuth, OidGroup.EnhancedKeyUsage),
                }, true)
            }
        };
        var altNames = new SubjectAlternativeNameBuilder();
        foreach (var dns in dnsNames)
            altNames.AddDnsName(dns);
        foreach (var ip in ipAddresses)
            altNames.AddIpAddress(IPAddress.Parse(ip));
        request.CertificateExtensions.Add(altNames.Build());
        var now = DateTimeOffset.Now;
        return request.Create(caCert, now, now.AddMonths(months), Guid.NewGuid().ToByteArray())
            .CopyWithPrivateKey(rsa);
    }

    private static void ExportCert(X509Certificate2 cert, string directory, string name)
    {
        static void WritePem(Stream stream, string label, byte[] data) =>
            stream.Write(PemEncoding.Write(label, data).Select(c => (byte)c).ToArray());
        var encoding = new UnicodeEncoding(false, false);
        using (var fs = new FileStream(Path.Combine(directory, $"{name}.crt"), FileMode.Create, FileAccess.Write))
            WritePem(fs, "CERTIFICATE", cert.RawData);
        using (var fs = new FileStream(Path.Combine(directory, $"{name}.key"), FileMode.Create, FileAccess.Write))
        using (var sw = new StreamWriter(fs, encoding))
        {
            if (cert.GetRSAPrivateKey() != null)
                WritePem(fs, "RSA PRIVATE KEY", cert.GetRSAPrivateKey()!.ExportRSAPrivateKey());
            else
                throw new NotSupportedException("Unknown private key");
        }
    }
}