using EventStoreProxy.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace EventStoreProxy;

public class ProxyController : Controller
{
    //See https://developers.eventstore.com/server/v21.10/security.html#default-users

    //Require auth for all endpoints normally available publicly
    //However, we have to allow anonymous access to /info, /ping and /web for the web UI to work
    //For all routes (include anonymous), forward straight to event store to avoid overhead of double authentication
    //Otherwise

    [Route("/gossip")]
    [Route("/stats")]
    [Route("/elections")]
    [Authorize(BasicAuthenticationDefaults.SchemeName)]
    public Task RequireAuth([FromServices] ProxyForwarder forwarder) => forwarder.Forward(HttpContext);

    [Route("{**catch-all}")]
    public Task CatchAll([FromServices] ProxyForwarder forwarder) => forwarder.Forward(HttpContext);
}