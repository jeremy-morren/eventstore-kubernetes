using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Forwarder;
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace EventStoreProxy;

public class ProxyForwarder
{
    private readonly IHttpForwarder _httpForwarder;
    private readonly ILogger<ProxyForwarder> _logger;
    private readonly IReadOnlyDictionary<string, string> _nodes;

    public ProxyForwarder(IHttpForwarder httpForwarder, 
        IConfiguration configuration,
        EventStoreNode[] nodes,
        ILogger<ProxyForwarder> logger)
    {
        _httpForwarder = httpForwarder;
        _logger = logger;
        configuration.GetSection("Proxy").Bind(_forwarderRequestConfig);
        _nodes = nodes.ToDictionary(n => n.PublicHost, n => n.InternalHost);
    }

    private readonly HttpMessageInvoker _httpClient = new(new SocketsHttpHandler()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current)
    });

    private readonly ForwarderRequestConfig _forwarderRequestConfig = new();

    public async Task Forward(HttpContext context, HttpTransformer transformer)
    {
        if (!_nodes.TryGetValue(context.Request.Host.ToString(), out var intHost))
            throw new Exception($"Unable to find matching internal HOST for public HOST {context.Request.Host}");
        var error = await _httpForwarder.SendAsync(context, $"https://{intHost}/", 
            _httpClient, _forwarderRequestConfig, transformer);
        if (error != ForwarderError.None)
        {
            var errorFeature = context.GetForwarderErrorFeature();
            var ex = errorFeature?.Exception;
            if (ex != null)
                _logger.LogWarning(ex, "An error occurred sending the request");
        }
    }
}