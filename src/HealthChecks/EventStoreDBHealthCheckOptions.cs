using System.Diagnostics.CodeAnalysis;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace HealthChecks.EventStoreDB.Grpc;

public class EventStoreDBHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the URLs use
    /// </summary>
    public ISet<string> URLs { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the credentials used to authenticate to EventStoreDB in format format <c>username:password</c>
    /// </summary>
    public string? Credentials { get; set; }

    /// <summary>
    /// Indicates whether the EventStore node(s) are secured with TLS (default <see langword="true"/>)
    /// </summary>
    public bool UseTLS { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the EventStore TLS certificates should be verified (default <see langword="true"/>)
    /// </summary>
    /// <remarks>
    /// If node(s) are using self-signed certificates, set to <see langword="true"/>, otherwise <see langword="false"/>
    /// </remarks>
    public bool VerifyTLSCert { get; set; } = true;

    /// <summary>
    /// Gets or sets whether HTTP logging should be enabled (default <see langword="false"/>)
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    public void Validate()
    {
        if (URLs == null! || URLs.Count == 0)
            throw new Exception("EventStore URL(s) are required");
    }

    public EventStoreClientSettings CreateSettings(ILoggerFactory loggerFactory)
    {
        Validate();
        var credentials = Credentials != null ? $"{Credentials}@" : null;
        var url = string.Join(",", URLs);
        var settings = EventStoreClientSettings.Create($"esdb://{credentials}{url}?tls={UseTLS}&tlsVerifyCert={VerifyTLSCert}");
        if (EnableLogging)
            settings.LoggerFactory = loggerFactory;
        return settings;
    }
}