using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using k8s.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

// ReSharper disable ClassNeverInstantiated.Global

namespace EventStoreBackup.Auth;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions>
{
    private readonly EventStoreClient _client;

    public BasicAuthenticationHandler(EventStoreClient client,
        IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) 
        : base(options, logger, encoder, clock) =>
        _client = client;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        //The header is 'Basic <base64encoded>'
        //The value is 'user:pass' (we will use Base64 Encoding)
        
        var src = Context.Request.Headers[HeaderNames.Authorization];
        if (src.Count != 1 || string.IsNullOrEmpty(src[0]))
            return AuthenticateResult.NoResult();
        
        var split = src[0]!.Split(' ');
        if (split.Length != 2)
            return AuthenticateResult.NoResult();
        
        var auth = Options.Encoding.GetString(Convert.FromBase64String(split[1])).Split(':');
        if (auth.Length != 2)
            return AuthenticateResult.NoResult();

        var ct = Context.RequestAborted;
        using var response = await _client.GetAsync(await Context.GetPodAsync(ct),
            "/users",
            new AuthenticationHeaderValue(Scheme.Name, split[1]),  //We send the raw value
            ct);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return AuthenticateResult.Fail("Unauthorized");
            response.EnsureSuccessStatusCode(); //Will throw something
        }
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var users = await JsonSerializer.DeserializeAsync<UserResponse>(stream, JsonOptions, ct);
        var user = users?.Data.FirstOrDefault(u => u.LoginName == auth[0]);
        if (user == null) 
            return AuthenticateResult.NoResult();
        if (user.Disabled)
            return AuthenticateResult.Fail("User disabled");
        var name = new Claim(ClaimTypes.Name, user.LoginName);
        var roles = user.Groups.Select(g => new Claim(ClaimTypes.Role, g));
        var identity = new ClaimsIdentity(new [] { name }.Concat(roles), Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    [JsonSerializable(typeof(UserResponse))]
    [JsonSerializable(typeof(User))]
    
    public record UserResponse(User[] Data);
    public record User(string LoginName, List<string> Groups, bool Disabled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}