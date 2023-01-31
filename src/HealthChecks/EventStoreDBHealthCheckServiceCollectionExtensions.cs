using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthChecks.EventStoreDB.Grpc;

public static class EventStoreDBHealthCheckServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddEventStore(this IHealthChecksBuilder builder,
        Action<EventStoreDBHealthCheckOptions> configureOptions,
        string name = "EventStore",
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        builder.Services.TryAddSingleton(typeof(EventStoreClientCollection));
        
        builder.Services.AddOptions<EventStoreDBHealthCheckOptions>(name)
            .Configure(configureOptions)
            .Validate(o =>
            {
                o.Validate();
                o.CreateSettings(NullLoggerFactory.Instance);
                return true;
            });
        
        return builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                var options = sp.GetRequiredService<IOptionsFactory<EventStoreDBHealthCheckOptions>>().Create(name);
                var clients = sp.GetRequiredService<EventStoreClientCollection>();
                var client = clients.GetOrAdd(name, () =>
                {
                    var settings = options.CreateSettings(loggerFactory);
                    return new EventStoreClient(settings);
                });
                return new EventStoreDBHealthCheck(client, options, loggerFactory.CreateLogger<EventStoreDBHealthCheck>());
            },
            failureStatus,
            tags,
            timeout));
    }
    
    public static IHealthChecksBuilder AddEventStore(this IHealthChecksBuilder builder,
        Action<IServiceProvider, EventStoreDBHealthCheckOptions> configureOptions,
        string name = "EventStore",
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        builder.Services.AddSingleton<IConfigureOptions<EventStoreDBHealthCheckOptions>>(sp => 
            new ConfigureNamedOptions<EventStoreDBHealthCheckOptions>(name, options => configureOptions(sp, options)));
        
        builder.Services.AddOptions<EventStoreDBHealthCheckOptions>()
            .Validate(o =>
            {
                o.Validate();
                o.CreateSettings(NullLoggerFactory.Instance);
                return true;
            });
        
        builder.Services.AddSingleton<EventStoreDBHealthCheck>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EventStoreDBHealthCheckOptions>>();
            var loggerFactory = sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var settings = options.Value.CreateSettings(loggerFactory);
            return new EventStoreDBHealthCheck(new EventStoreClient(settings),
                new EventStoreDBHealthCheckOptions(),
                NullLogger<EventStoreDBHealthCheck>.Instance);
        });
        
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<EventStoreDBHealthCheck>(),
            failureStatus,
            tags,
            timeout));
    }
}