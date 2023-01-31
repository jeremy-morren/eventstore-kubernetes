using EventStore.Client;
using Microsoft.Extensions.Logging;

// ReSharper disable StringLiteralTypo
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
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
    public IList<string> Nodes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the credentials used to authenticate to EventStoreDB in format <c>username:password</c>
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

    /// <summary>
    /// The stream to read.
    /// </summary>
    /// <remarks>
    /// It is not necessary for the stream to exist.
    /// </remarks>
    public string HealthCheckStream { get; set; } = "$health";

    public void Validate()
    {
        if (Nodes == null! || Nodes.Count == 0)
            throw new Exception("EventStore Nodes(s) are required");
    }

    /// <summary>
    /// Creates a new instance of <see cref="EventStoreClientSettings"/>, with logging
    /// from the provided <see cref="ILoggerFactory"/>
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use for logging</param>
    /// <returns></returns>
    public EventStoreClientSettings CreateSettings(ILoggerFactory loggerFactory)
    {
        Validate();
        var credentials = Credentials != null ? $"{Credentials}@" : null;
        var host = string.Join(",", Nodes);
        var settings = EventStoreClientSettings.Create($"esdb://{credentials}{host}?tls={UseTLS}&tlsVerifyCert={VerifyTLSCert}");
        if (EnableLogging)
            settings.LoggerFactory = loggerFactory;
        return settings;
    }
}