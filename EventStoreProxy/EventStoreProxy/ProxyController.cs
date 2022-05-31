using EventStoreProxy.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Forwarder;
// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace EventStoreProxy;

public class ProxyController : Controller
{
    private readonly EventStoreNode[] _nodes;
    private readonly ProxyForwarder _forwarder;

    public ProxyController(EventStoreNode[] nodes,
        ProxyForwarder forwarder)
    {
        _nodes = nodes;
        _forwarder = forwarder;
    }
    
    //See https://developers.eventstore.com/server/v21.10/security.html#default-users
    //Require auth for all endpoints publicly
    //However, we have to allow anonymous access to /info for the web UI to work
    [Route("/ping")]
    [Route("/stats")]
    [Route("/elections")]
    [Route("/gossip")]
    [Authorize(BasicAuthenticationDefaults.SchemeName), EnableCors]
    public Task RequireAuth() => _forwarder.Forward(HttpContext, HttpTransformer.Default);

    [Route("{**catch-all}"), EnableCors]
    public Task CatchAll() => _forwarder.Forward(HttpContext, HttpTransformer.Default);
}