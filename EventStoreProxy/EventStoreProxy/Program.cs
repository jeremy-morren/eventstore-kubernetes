using EventStoreProxy;
using EventStoreProxy.Authentication;
using Microsoft.AspNetCore.HttpsPolicy;
using Serilog;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;


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
        .WriteTo.Console(outputTemplate: logTemplate));

    builder.Services.AddCors(options =>
    {
        var clusterUrls = builder.Configuration["EventStore:ClusterUrls"]
                              .Split(",", StringSplitOptions.TrimEntries)
                          ?? throw new InvalidOperationException("EventStore:ClusterUrls not configured");
        Log.Information("Setting up CORS with Cluster URLs {@ClusterUrls}", clusterUrls);
        options.DefaultPolicyName = "ClusterPolicy";
        options.AddPolicy("ClusterPolicy", b => b.WithOrigins(clusterUrls)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    builder.Services.AddCertificateAuthentication(builder.Configuration.GetSection("EventStore:Certificates"));

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddTransforms(builderContext => builderContext.UseESTrustedAuth());

    var app = builder.Build();
    
    app.UseHttpsRedirection();
    
    app.UseHsts();

    app.UseRouting();

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAuthEndpoint();

    app.MapReverseProxy();

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
