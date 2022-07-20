using Microsoft.Net.Http.Headers;

namespace EventStoreProxy;

public static class Cors
{
    private static readonly string[] Headers =
    {
        HeaderNames.ContentType,
        HeaderNames.Authorization,

        "ES-LongPoll",
        "ES-ExpectedVersion",
        "ES-EventId",
        "ES-EventType",
        "ES-RequireMaster",
        "ES-RequireLeader",
        "ES-HardDelete",
        "ES-ResolveLinkTos"
    };

    private static readonly string[] ExposedHeaders =
    {
        HeaderNames.Location,
        HeaderNames.Authorization,

        "ES-Position",
        "ES-CurrentVersion"
    };

    public static IServiceCollection AddCors(this IServiceCollection services, IEnumerable<EventStoreNode> nodes)
    {
        var origins = nodes.Select(n => $"https://{n.PublicHost}").ToArray();

        services.AddCors(o =>
        {
            o.AddDefaultPolicy(b => b
                .WithOrigins(origins)
                .AllowCredentials()
                .WithHeaders(Headers)
                .WithExposedHeaders(ExposedHeaders)
                .WithMethods("GET", "OPTIONS"));
        });

        return services;
    }
}