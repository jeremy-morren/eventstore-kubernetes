using Microsoft.AspNetCore.Authentication;

namespace EventStoreProxy.Authentication;

public class BasicAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public EventStoreNode[] Nodes { get; set; } = null!;
}