using System.Net;
using EventStoreProxy;
using EventStoreProxy.Authentication;
using EventStoreProxy.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;

const string logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: logTemplate)
    .CreateBootstrapLogger();

try
{
    Log.Debug("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables("PROXY_");

    builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

    builder.Host.UseSerilog((context, services, conf) => conf
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console(outputTemplate: logTemplate));

    var nodes = new List<EventStoreNode>();
    builder.Configuration.GetSection("EventStore").Bind(nodes);
    if (nodes.Count == 0)
        throw new InvalidOperationException("Unable to get cluster nodes config");

    Log.Information("Setting up proxy with cluster nodes {@ClusterNodes}", nodes);
    builder.Services
        .AddSingleton(nodes.ToArray())
        .AddCors(nodes);

    builder.Services
        .AddHttpForwarder()
        .AddSingleton<ProxyForwarder>()
        .AddSingleton<ProxyTransformer>()
        .AddHealthChecks();

    builder.Services.AddControllers();

    builder.Services.AddBasicAuthentication();

    builder.Services.Configure<List<EventStoreNode>>(builder.Configuration.GetSection("Proxy"));

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions()
    {
        ForwardedHeaders = ForwardedHeaders.All
    });

    app.UseHttpsRedirection();

    app.UseHsts();

    app.UseSerilogRequestLogging(o =>
    {
        o.GetLevel = (c, _, _) => c.Request.Path.StartsWithSegments("/healthz/live")
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    });

    app.UseCors();

    app.MapHealthChecks("/healthz/live", HttpStatusCode.ServiceUnavailable);

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Error starting web host");
}
finally
{
    Log.CloseAndFlush();
}