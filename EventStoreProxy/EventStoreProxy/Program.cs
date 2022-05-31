using System.Net;
using EventStoreProxy;
using EventStoreProxy.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

const string logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: logTemplate)
    .CreateBootstrapLogger();

try
{
    Log.Debug("Starting web host");
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Configuration.AddEnvironmentVariables("PROXY_");

    if (string.IsNullOrWhiteSpace(builder.Configuration["AcmeHost"]))
        throw new InvalidOperationException("AcmeHost configuration not found");

    builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

    builder.Host.UseSerilog((context, conf) => conf
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(outputTemplate: logTemplate));

    var nodes = new List<EventStoreNode>();
    builder.Configuration.GetSection("EventStore").Bind(nodes);
    if (nodes.Count == 0)
        throw new InvalidOperationException("Unable to get cluster nodes config");

    Log.Information("Setting up proxy with cluster nodes {@ClusterNodes}", nodes);
    builder.Services
        .AddSingleton(nodes.ToArray())
        .AddHttpForwarder()
        .AddSingleton<ProxyForwarder>();

    builder.Services.AddControllers();

    builder.Services.AddCors();
    
    builder.Services.AddHttpClient<BasicAuthenticationHandler>();

    builder.Services
        .AddAuthentication(BasicAuthenticationDefaults.SchemeName)
        .AddScheme<BasicAuthenticationHandlerOptions, BasicAuthenticationHandler>(
            BasicAuthenticationDefaults.SchemeName, o=> o.Nodes = nodes.ToArray());

    builder.Services.AddAuthorization(o => 
        o.AddPolicy(BasicAuthenticationDefaults.SchemeName, new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(BasicAuthenticationDefaults.SchemeName)
            .RequireAuthenticatedUser()
            .Build()));
    
    builder.Services.AddHealthChecks();

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
    
    app.UseCors(b =>
    {
        var origins = nodes.Select(n => $"https://{n.PublicHost}/").ToArray();
        b.WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

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
