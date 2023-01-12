using Microsoft.AspNetCore.Authentication;

namespace EventStoreBackup.Auth;

public static class BasicAuthenticationServiceCollectionExtensions
{
    public static AuthenticationBuilder AddBasic(this AuthenticationBuilder builder,
        string scheme,
        Action<BasicAuthenticationSchemeOptions>? configureOptions)
    {
        builder.Services.AddAuthentication(scheme)
            .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(scheme,
                options => configureOptions?.Invoke(options));

        return builder;
    }
}