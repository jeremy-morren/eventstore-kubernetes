using Yarp.ReverseProxy.Forwarder;

namespace EventStoreProxy;

public class ProxyTransformer : HttpTransformer
{
    public override ValueTask TransformRequestAsync(HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix) =>
        Default.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

    public override ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse)
    {
        if (proxyResponse == null)
            return Default.TransformResponseAsync(httpContext, proxyResponse);

        //Remove cors headers
        var corsHeaders = proxyResponse.Headers
            .Where(h => h.Key.StartsWith("Access-Control", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
        foreach (var h in corsHeaders)
            proxyResponse.Headers.Remove(h.Key);

        return Default.TransformResponseAsync(httpContext, proxyResponse);
    }

    public override ValueTask
        TransformResponseTrailersAsync(HttpContext httpContext, HttpResponseMessage proxyResponse) =>
        Default.TransformResponseTrailersAsync(httpContext, proxyResponse);
}