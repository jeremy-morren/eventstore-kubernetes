using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using k8s;
using k8s.Models;
using Serilog;

namespace ClientCertificatesCli;

public class CertificatesService
{
    private readonly string _namespace;
    private readonly string _secretName;
    private readonly Kubernetes _client;

    public CertificatesService(KubernetesClientConfiguration config,
        string? @namespace, 
        string? secretName)
    {
        _namespace = @namespace ?? config.Namespace ?? "default";
        if (string.IsNullOrWhiteSpace(secretName))
            throw new InvalidOperationException("Secret name not provided");
        _secretName = secretName;
        _client = new Kubernetes(config);
    }

    public static readonly Encoding AuthEncoding = Encoding.UTF8;

    public IEnumerable<ClientCertificate> GetCertificates()
    {
        try
        {
            var secret = _client.CoreV1.ReadNamespacedSecret(_secretName, _namespace);
            if (secret.Data == null) return Array.Empty<ClientCertificate>();
            return secret.Data.Keys
                .Where(k => k.EndsWith(".pfx"))
                .Select(n =>
                {
                    var cert = new X509Certificate2(secret.Data[n], ReadOnlySpan<char>.Empty, X509KeyStorageFlags.Exportable);
                    var name = cert.GetNameInfo(X509NameType.SimpleName, false);
                    var auth = secret.Data[$"{cert.Thumbprint}.auth"];
                    return new ClientCertificate(name, cert, AuthEncoding.GetString(auth));
                });
        }
        catch (k8s.Autorest.HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<ClientCertificate>();
        }
    }

    public void Save(IEnumerable<ClientCertificate> certificates)
    {
        var data = new Dictionary<string, byte[]>();
        foreach (var cert in certificates)
        {
            data.Add($"{cert.Certificate.Thumbprint}.pfx", cert.Certificate.Export(X509ContentType.Pkcs12));
            data.Add($"{cert.Certificate.Thumbprint}.auth", AuthEncoding.GetBytes(cert.Auth));
        }
        V1Secret? current;
        try
        {
            current = _client.CoreV1.ReadNamespacedSecret(_secretName, _namespace);
        }
        catch (k8s.Autorest.HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            Log.Debug("Secret not found");
            current = null;
        }
        var secret = new V1Secret(kind: "Secret",
            type: "Opaque",
            data: data, 
            immutable: true,
            metadata: new V1ObjectMeta()
            {
                Name = _secretName,
                NamespaceProperty = _namespace,
                Labels = current?.Metadata.Labels,
                Annotations = current?.Metadata.Annotations
            });
        if (current != null)
            _client.CoreV1.DeleteNamespacedSecret(_secretName, _namespace);
        _client.CoreV1.CreateNamespacedSecret(secret, _namespace);
        Log.Debug("Updated certificates secret");
    }
}

public record ClientCertificate(string Name, X509Certificate2 Certificate, string Auth)
{
    public string Auth { get; set; } = Auth;
}