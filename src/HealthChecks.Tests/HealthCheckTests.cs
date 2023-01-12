using EventStore.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HealthChecks.EventStoreDB.Grpc.Tests;

public class HealthCheckTests : IClassFixture<EventStoreTestHarness>
{
    private readonly EventStoreTestHarness _harness;

    public HealthCheckTests(EventStoreTestHarness harness) => _harness = harness;

    [Fact]
    public async Task Failure_Grpc_Error()
    {
        await using var _ = Create("esdb://localhost:50?tls=false", 
            HealthStatus.Unhealthy,
            out var healthCheck,
            out var context);
        var token = new CancellationTokenSource(5000).Token;
        var result = await healthCheck.CheckHealthAsync(context, token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("GrpcError", result.Description);
    }
    
    [Fact]
    public async Task Failure_Timeout()
    {
        await using var _ = Create("esdb://localhost:51?tls=false", 
            HealthStatus.Unhealthy,
            out var healthCheck,
            out var context);
        var token = new CancellationTokenSource(500).Token;
        var result = await healthCheck.CheckHealthAsync(context, token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Timeout", result.Description);
    }
    
    [Fact]
    public async Task Success()
    {
        await using var _ = Create($"esdb://localhost:{_harness.Port}?tls=false", 
            HealthStatus.Unhealthy,
            out var healthCheck,
            out var context);
        var token = new CancellationTokenSource(500).Token;
        var result = await healthCheck.CheckHealthAsync(context, token);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private static EventStoreClient Create(string connString, 
        HealthStatus failureStatus,
        out EventStoreDBHealthCheck healthCheck,
        out HealthCheckContext context)
    {
        var registration = new HealthCheckRegistration("EventStore", 
            instance: new Mock<IHealthCheck>().Object, 
            failureStatus: failureStatus,
            tags: null, 
            timeout: null);
        context = new HealthCheckContext() {Registration = registration};
        var client = new EventStoreClient(EventStoreClientSettings.Create(connString));
        healthCheck = new EventStoreDBHealthCheck(client, 
            new EventStoreDBHealthCheckOptions(),
            NullLogger<EventStoreDBHealthCheck>.Instance);
        return client;
    }
}