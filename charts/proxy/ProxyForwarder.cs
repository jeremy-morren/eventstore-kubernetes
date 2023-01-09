using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace EventStoreProxy;

public class ProxyForwarder
{
    private readonly ForwarderRequestConfig _forwarderRequestConfig = new()
    {
        Version = new Version(2,0),
        VersionPolicy = HttpVersionPolicy.RequestVersionExact
    };

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
    private readonly ProxyTransformer _transformer;

    public ProxyForwarder(IHttpForwarder httpForwarder,
        ILogger<ProxyForwarder> logger,
        ProxyTransformer transformer)
    {
        _httpForwarder = httpForwarder;
        _logger = logger;
        _transformer = transformer;
    }

    public async Task Forward(HttpContext context)
    {
        var error = await _httpForwarder.SendAsync(context, 
            "https://localhost:2113/",
            _httpClient,
            _forwarderRequestConfig, 
            _transformer);
        if (error != ForwarderError.None)
        {
            var errorFeature = context.GetForwarderErrorFeature();
            var ex = errorFeature?.Exception;
            if (ex != null)
                _logger.LogWarning(ex, "An error occurred sending the request");
        }
    }
}