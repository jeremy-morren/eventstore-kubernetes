using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EventStoreProxy.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationHandlerOptions>
{
    private readonly HttpClient _httpClient;

    public BasicAuthenticationHandler(HttpClient httpClient,
        IOptionsMonitor<BasicAuthenticationHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _httpClient = httpClient;
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
        if (!await Authenticate(0, auth.ToString(), Context.RequestAborted))
            return AuthenticateResult.Fail("Invalid username/password");
        var claims = new Claim[]
        {
            new(ClaimTypes.Name, username)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task<bool> Authenticate(int i, string auth, CancellationToken cancellationToken)
    {
        //TODO: Cache authentication result
        if (i == Options.Nodes.Length)
            throw new AllNodesUnreachableException();
        var @base = new Uri($"https://{Options.Nodes[i].InternalHost}", UriKind.Absolute);
        var url = new Uri(@base, "/info");
        try
        {
            //Probe /info
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = url,
                Headers =
                {
                    Authorization = AuthenticationHeaderValue.Parse(auth)
                }
            };
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK) return true;
            Logger.LogWarning("HTTP GET {Url} returned {StatusCode}", url.ToString(), response.StatusCode);
            return false;
        }
        catch (HttpRequestException e)
        {
            Logger.LogWarning(e, "HTTP GET {Url} failed, retrying", url.ToString());
            return await Authenticate(i + 1, auth, cancellationToken);
        }
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
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool ConstantTimeEqual(byte[] x, byte[] y)
    {
        // Based on https://github.com/sdrapkin/SecurityDriven.Inferno/blob/master/Utils.cs#L48
        var difBits = 0;
        unchecked
        {
            for (var i = 0; i < Math.Max(x.Length, y.Length); ++i)
            {
                var xByte = i < x.Length ? x[i] : (byte)0;
                var yByte = i < y.Length ? y[i] : (byte)0;
                difBits |= xByte ^ yByte;
            }
        }
        return difBits == 0;
    }
}