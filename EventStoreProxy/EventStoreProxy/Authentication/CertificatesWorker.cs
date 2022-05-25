using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;

namespace EventStoreProxy.Authentication;

public class CertificatesWorker : BackgroundService
{
    private readonly ILogger<CertificatesWorker> _logger;
    private readonly ISystemClock _systemClock;
    private readonly IConfiguration _configuration;
    private readonly CertificateValidationCacheOptions _cacheOptions;

    public CertificatesWorker(ILogger<CertificatesWorker> logger,
        IOptions<CertificateValidationCacheOptions> cacheOptions,
        ISystemClock systemClock,
        IConfiguration configuration)
    {
        _logger = logger;
        _systemClock = systemClock;
        _configuration = configuration;
        _cacheOptions = cacheOptions.Value;
    }

    private void CreateClient(out Kubernetes client, out string ns, out string secret)
    {
        var settings = KubernetesClientConfiguration.BuildDefaultConfig();
        client = new Kubernetes(settings);
        ns = settings.Namespace ?? "default";
        secret = _configuration["ClientCerts"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("CLIENTCERTS not configured");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            CreateClient(out var client, out var ns, out var secretName);
            var resp = client.CoreV1
                .ListNamespacedSecretWithHttpMessagesAsync(ns, watch: true, cancellationToken: stoppingToken);
            //Because the secret is deleted and recreated, we can ignore delete events
            await foreach (var (type, secret) in resp.WatchAsync<V1Secret, V1SecretList>().WithCancellation(stoppingToken))
                if (type != WatchEventType.Deleted && secret.Metadata.Name == secretName)
                    await RefreshCertificates();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error watching certificates secret");
            throw;
        }
    }
    
    private async Task<Dictionary<string, ClientCertificate>> LoadCertificates()
    {
        try
        {
            CreateClient(out var client, out var ns, out var secretName);
            var secret = await client.CoreV1.ReadNamespacedSecretAsync(secretName, ns);
            if (secret.Data == null) return new Dictionary<string, ClientCertificate>();
            return secret.Data.Keys
                .Where(k => k.EndsWith(".pfx"))
                .Select(n =>
                {
                    var cert = new X509Certificate2(secret.Data[n]);
                    return new ClientCertificate(cert,
                        cert.GetNameInfo(X509NameType.SimpleName, false),
                        Encoding.UTF8.GetString(secret.Data[$"{cert.Thumbprint}.auth"]));
                })
                .ToDictionary(c => c.Certificate.Thumbprint);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading certificates");
            throw;
        }
    }

    private Dictionary<string, ClientCertificate>? _certificates;
    private DateTimeOffset? _lastRefreshed;

    private async Task RefreshCertificates()
    {
        _certificates = await LoadCertificates();
        _lastRefreshed = _systemClock.UtcNow;
        _logger.LogInformation("Loaded {Count} Certificates", _certificates.Count);
    }

    public async ValueTask<IReadOnlyDictionary<string, ClientCertificate>> GetClientCertificates()
    {
        if (_certificates != null &&
            _lastRefreshed.HasValue
            && (_systemClock.UtcNow - _lastRefreshed.Value) < _cacheOptions.CacheEntryExpiration)
            return _certificates;
        await RefreshCertificates();
        return _certificates!;
    }

    public record ClientCertificate(X509Certificate2 Certificate, string Name, string Auth);
}