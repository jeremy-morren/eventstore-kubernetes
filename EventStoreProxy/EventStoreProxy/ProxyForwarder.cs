using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace EventStoreProxy;

public class ProxyForwarder
{
    private readonly ForwarderRequestConfig _forwarderRequestConfig = new();

    private readonly HttpMessageInvoker _httpClient = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current)
    });

    private readonly IHttpForwarder _httpForwarder;
    private readonly ILogger<ProxyForwarder> _logger;
    private readonly IReadOnlyDictionary<string, string> _nodes;
    private readonly ProxyTransformer _transformer;

    public ProxyForwarder(IHttpForwarder httpForwarder,
        IConfiguration configuration,
        EventStoreNode[] nodes,
        ILogger<ProxyForwarder> logger,
        ProxyTransformer transformer)
    {
        _httpForwarder = httpForwarder;
        _logger = logger;
        _transformer = transformer;
        configuration.GetSection("Proxy").Bind(_forwarderRequestConfig);
        _nodes = nodes.ToDictionary(n => n.PublicHost, n => n.InternalHost);
    }

    public async Task Forward(HttpContext context)
    {
        if (!_nodes.TryGetValue(context.Request.Host.ToString(), out var intHost))
            throw new Exception($"Unable to find matching internal HOST for public HOST {context.Request.Host}");
        var error = await _httpForwarder.SendAsync(context, $"https://{intHost}/",
            _httpClient, _forwarderRequestConfig, _transformer);
        if (error != ForwarderError.None)
        {
            var errorFeature = context.GetForwarderErrorFeature();
            var ex = errorFeature?.Exception;
            if (ex != null)
                _logger.LogWarning(ex, "An error occurred sending the request");
        }
    }
}