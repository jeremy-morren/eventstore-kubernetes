using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.EventStoreDB.Grpc;

public sealed class EventStoreDBHealthCheck : IHealthCheck, IAsyncDisposable
{
    private readonly EventStoreClient _client;

    public EventStoreDBHealthCheck(EventStoreClient client) =>
        _client = client ?? throw new ArgumentNullException(nameof(client));

    public ValueTask DisposeAsync() => _client.DisposeAsync();

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = new ())
    {
        try
        {
            //Read a $health stream
            //We don't actually care about the result (usually 'NotExists')
            //Even if it returns 'NotFound' that means we connected
            await _client.ReadStreamAsync(Direction.Forwards,
                "$health",
                StreamPosition.Start,
                maxCount: 1,
                deadline: null,
                resolveLinkTos: false,
                cancellationToken: ct).ReadState;
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException e)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Timeout", e);
        }
        catch (RpcException e)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "GrpcError", e);
        }
        catch (Exception e)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Error", e);
        }
    }
}