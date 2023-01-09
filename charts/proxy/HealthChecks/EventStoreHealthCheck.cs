using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventStoreProxy.HealthChecks;

public class EventStoreHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventStoreHealthCheck> _logger;

    public EventStoreHealthCheck(HttpClient httpClient,
        ILogger<EventStoreHealthCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken ct = new())
    {
        // Ensure we can connect to the eventstore container
        // Probe /info for liveness
        // /health/live fails when the cluster is invalid (see StatefulSet liveness probe)
        try
        {
            using var response = await _httpClient.GetAsync("https://localhost:2113/info", ct);
            response.EnsureSuccessStatusCode();
            return HealthCheckResult.Healthy();
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("Failed to reach eventstore node", e);
        }
    }
}