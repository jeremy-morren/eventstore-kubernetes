using System.Net;
using EventStoreProxy;
using EventStoreProxy.Authentication;
using Serilog;
using Serilog.Events;

const string logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: logTemplate)
    .CreateBootstrapLogger();

try
{
    Log.Debug("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables("PROXY_");

    builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

    builder.Host.UseSerilog((context, conf) => conf
        .ReadFrom.Configuration(context.Configuration)
        //Filter healthcheck endpoints
        .Filter.ByExcluding(e =>
            e.Properties.TryGetValue("RequestPath", out var path)
            && path is ScalarValue {Value: string pathStr}
            && pathStr.StartsWith("/healthz"))
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
        .AddSingleton<ProxyTransformer>();

    builder.Services.AddControllers();

    builder.Services.AddBasicAuthentication();

    builder.Services.AddHealthChecks();

    builder.Services.Configure<List<EventStoreNode>>(builder.Configuration.GetSection("Proxy"));

    var app = builder.Build();

    app.UseHttpsRedirection();

    app.UseHsts();

    app.Use(async (HttpContext context, RequestDelegate next) =>
    {
        try
        {
            await next(context);
        }
        catch (AllNodesUnreachableException e)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(BasicAuthenticationHandler));
            logger.LogError(e, "All cluster nodes unreachable for authentication");
            context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync(e.Message);
        }
    });

    app.UseRouting();

    app.MapHealthChecks("/healthz/live");

    app.UseCors();

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