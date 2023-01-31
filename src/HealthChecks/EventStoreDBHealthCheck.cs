using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace HealthChecks.EventStoreDB.Grpc;

public sealed class EventStoreDBHealthCheck : IHealthCheck
{
    private readonly EventStoreClient _client;
    private readonly EventStoreDBHealthCheckOptions _options;
    private readonly ILogger<EventStoreDBHealthCheck> _logger;

    public EventStoreDBHealthCheck(EventStoreClient client,
        EventStoreDBHealthCheckOptions options,
        ILogger<EventStoreDBHealthCheck> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = new ())
    {
        try
        {
            //Read a $health stream
            //We don't actually care about the result (usually 'NotExists')
            //Even if it returns 'NotFound' that means we connected
            var readState = await _client.ReadStreamAsync(Direction.Forwards,
                _options.HealthCheckStream,
                StreamPosition.Start,
                maxCount: 1,
                deadline: null,
                resolveLinkTos: false,
                cancellationToken: ct).ReadState;
            _logger.LogDebug("Read stream {Stream} returned {ReadState}", _options.HealthCheckStream, readState);
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