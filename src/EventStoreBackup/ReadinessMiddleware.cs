using System.Net;
using System.Net.Sockets;

namespace EventStoreBackup;

public class ReadinessMiddleware
{
    private readonly ILogger<ReadinessMiddleware> _logger;
    private readonly EventStoreClient _client;
    private readonly RequestDelegate _next;

    public ReadinessMiddleware(ILogger<ReadinessMiddleware> logger,
        EventStoreClient client,
        RequestDelegate next)
    {
        _logger = logger;
        _client = client;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            var ct = context.RequestAborted;
            if (!context.Request.Path.ToString().StartsWith("/healthz"))
            {
                var pod = await context.GetPodAsync(ct);
                if (pod.Status.ContainerStatuses.Any(c => !c.Ready))
                {
                    _logger.LogError("Pod {@Pod} is not ready", pod);
                    context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
                    return;
                }
                //Check readiness
                using var response = await _client.GetAsync(pod, "/info", null, ct);
                response.EnsureSuccessStatusCode();
            }

            await _next(context);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(e, "Downstream HTTP request returned {Code}", (int) e.StatusCode);
            if (!context.Response.HasStarted)
                context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
        }
        catch (HttpRequestException e) when (e.InnerException is SocketException)
        {
            _logger.LogError(e, "Downstream HTTP request returned SocketException");
            if (!context.Response.HasStarted)
                context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
        }
    }
}