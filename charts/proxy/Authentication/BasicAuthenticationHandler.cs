using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventStoreProxy.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationHandlerOptions>
{
    private readonly HttpClient _httpClient;
    private readonly EventStoreNode[] _nodes;

    public BasicAuthenticationHandler(HttpClient httpClient,
        EventStoreNode[] nodes,
        IOptionsMonitor<BasicAuthenticationHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _httpClient = httpClient;
        _nodes = nodes;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        //TODO: Add Cache
        var auth = Context.Request.Headers.Authorization;
        if (auth.Count == 0)
            return AuthenticateResult.NoResult();
        if (!Parse(auth.ToString(), out var username))
            return AuthenticateResult.NoResult();
        Logger.LogInformation("Authenticating user {Username}", username);
        if (!await Authenticate(AuthenticationHeaderValue.Parse(auth.ToString()), Context.RequestAborted))
            return AuthenticateResult.Fail("Invalid username/password");
        var claims = new Claim[]
        {
            new(ClaimTypes.Name, username)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task<bool> Authenticate(AuthenticationHeaderValue auth, CancellationToken cancellationToken)
    {
        //Probe /info
        //Although it is unsecured, if we send request with an auth header then the challenge is processed
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://localhost:2113/info"),
            Headers =
            {
                Authorization = auth
            }
        };
        using var response = await _httpClient.SendAsync(message, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private bool Parse(string input, out string username)
    {
        if (input.StartsWith(Scheme.Name))
            input = input[Scheme.Name.Length..].Trim();
        var auth = Convert.FromBase64String(input);
        var content = Encoding.ASCII.GetString(auth);
        var index = content.IndexOf(':');
        if (index == -1)
        {
            username = null!;
            return false;
        }

        username = content[..index];
        return true;
    }
}