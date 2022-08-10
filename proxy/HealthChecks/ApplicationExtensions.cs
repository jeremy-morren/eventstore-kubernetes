using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventStoreProxy.HealthChecks;

public static class ApplicationExtensions
{
    public static IHealthChecksBuilder AddAppHealthChecks(this WebApplicationBuilder app) 
        => app.Services
            .AddHealthChecks()
            .AddCheck<EventStoreHealthCheck>("EventStore", HealthStatus.Unhealthy);

    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder app, 
        string pattern,
        HttpStatusCode degraded)
    {
        app.MapHealthChecks(pattern, new HealthCheckOptions()
        {
            ResultStatusCodes = new Dictionary<HealthStatus, int>()
            {
                {HealthStatus.Healthy, (int) HttpStatusCode.OK},
                {HealthStatus.Degraded, (int) degraded},
                {HealthStatus.Unhealthy, (int) HttpStatusCode.ServiceUnavailable}
            }
        }).AllowAnonymous();
        return app;
    }
}