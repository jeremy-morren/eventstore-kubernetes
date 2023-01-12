using EventStore.Client;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthChecks.EventStoreDB.Grpc.Tests;

public class OptionsTests : IClassFixture<EventStoreTestHarness>
{
    private readonly EventStoreTestHarness _harness;

    public OptionsTests(EventStoreTestHarness harness) => _harness = harness;

    [Fact]
    public void Validate_Without_Nodes_Should_Fail()
    {
        var options = new EventStoreDBHealthCheckOptions();
        Assert.ThrowsAny<Exception>(() => options.Validate());
        Assert.ThrowsAny<Exception>(() => options.CreateSettings(NullLoggerFactory.Instance));
    }
    
    [Fact]
    public void Validate_With_Nodes_Should_Succeed()
    {
        var options = new EventStoreDBHealthCheckOptions()
        {
            Nodes =
            {
                "localhost",
                "example.com:2345"
            }
        };
        options.Validate();
        options.CreateSettings(NullLoggerFactory.Instance);
    }
    
    [Fact]
    public async Task Connection_Should_Succeed()
    {
        var options = new EventStoreDBHealthCheckOptions()
        {
            Nodes = {$"localhost:{_harness.Port}" },
            UseTLS = false
        };
        var settings = options.CreateSettings(NullLoggerFactory.Instance);
        await using var client = new EventStoreClient(settings);
        await client.ReadAllAsync(Direction.Forwards, Position.Start).FirstOrDefaultAsync();
    }
}