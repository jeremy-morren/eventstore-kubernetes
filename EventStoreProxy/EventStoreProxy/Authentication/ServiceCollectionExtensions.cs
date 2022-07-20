using Microsoft.AspNetCore.Authorization;

namespace EventStoreProxy.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBasicAuthentication(this IServiceCollection services)
    {
        services.AddHttpClient<BasicAuthenticationHandler>();

        services
            .AddAuthentication(BasicAuthenticationDefaults.SchemeName)
            .AddScheme<BasicAuthenticationHandlerOptions, BasicAuthenticationHandler>(
                BasicAuthenticationDefaults.SchemeName, o => { });

        services.AddAuthorization(o =>
            o.AddPolicy(BasicAuthenticationDefaults.SchemeName, new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(BasicAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .Build()));

        return services;
    }
}