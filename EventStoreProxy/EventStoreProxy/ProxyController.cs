using EventStoreProxy.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Forwarder;
// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace EventStoreProxy;

public class ProxyController : Controller
{
    //See https://developers.eventstore.com/server/v21.10/security.html#default-users
    //Require auth for all endpoints publicly
    //However, we have to allow anonymous access to /info for the web UI to work
    [Route("/gossip")]
    [Route("/ping")]
    [Route("/stats")]
    [Route("/elections")]
    [Authorize(BasicAuthenticationDefaults.SchemeName)]
    public Task RequireAuth([FromServices] ProxyForwarder forwarder) => forwarder.Forward(HttpContext, HttpTransformer.Default);

    [Route("{**catch-all}")]
    public Task CatchAll([FromServices] ProxyForwarder forwarder) => forwarder.Forward(HttpContext, HttpTransformer.Default);
    
    [Route("/.well-known/acme-challenge")]
    public IActionResult AcmeHandler([FromServices] IConfiguration configuration,
        ILogger<ProxyController> logger)
    {
        var @base = new Uri(configuration["AcmeHandler"], UriKind.Absolute);
        var destination = new Uri(@base, HttpContext.Request.GetEncodedPathAndQuery());
        logger.LogInformation("Redirecting ACME challenge to {Destination}", destination);
        return Redirect(destination.ToString());
    }
}