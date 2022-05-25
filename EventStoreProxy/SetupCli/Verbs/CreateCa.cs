using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using k8s;
using k8s.Models;
using Serilog;

namespace EventStore.Setup.Verbs;

[Verb("create-ca", HelpText = "Gets/creates the CA certificate")]
public class CreateCa
{
    public CreateCa(string secretName, string clusterName, int length)
    {
        SecretName = secretName;
        ClusterName = clusterName;
        Length = length;
    }

    [Option('s', "secret", Required = true, HelpText = "CA certificate secret name")]
    public string SecretName { get; }

    [Option('n', "name", Required = true, HelpText = "Cluster name (certificate subject)")]
    public string ClusterName { get; }

    [Option('l', "length", Required = false, Default = 10, HelpText = "The validity period in years")]
    public int Length { get; }

    public void Process()
    {
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        var client = new Kubernetes(config);
        var ns = config.Namespace ?? "default";
        var secret = client.CoreV1.ReadNamespacedSecret(SecretName, ns);
        if (secret.Data != null && secret.Data.TryGetValue("ca.crt", out var bytes) && bytes != null)
            throw new ArgumentException("CA certificate already exists");
        var cert = GenerateCaCert(ClusterName, Length);
        var @new = new V1Secret(kind: "Secret",
            type: "Opaque",
            data: new Dictionary<string, byte[]>()
            {
                {"ca.crt", ExportPem("CERTIFICATE", cert.RawData)},
                {"ca.key", ExportPem("RSA PRIVATE KEY", cert.GetRSAPrivateKey()!.ExportRSAPrivateKey())}
            },
            immutable: true,
            metadata: new V1ObjectMeta()
            {
                Name = secret.Metadata.Name,
                NamespaceProperty = secret.Metadata.NamespaceProperty,
                Labels = secret.Metadata.Labels,
                Annotations = secret.Metadata.Annotations
            });
        client.DeleteNamespacedSecret(SecretName, ns);
        client.CreateNamespacedSecret(@new, ns);
        Log.Information("Created new CA certificate {@Certificate}", new
        {
            Subject = ClusterName,
            cert.Thumbprint
        });
    }

    private static X509Certificate2 GenerateCaCert(string subject, int years, int keyStrength = 4096)
    {
        var subj = new X500DistinguishedName($"CN={subject}");
        using var rsa = RSA.Create(keyStrength);
        var request = new CertificateRequest(subj, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(years));
    }

    private static byte[] ExportPem(string label, byte[] data) =>
        PemEncoding.Write(label, data).Select(c => (byte) c).ToArray();
}