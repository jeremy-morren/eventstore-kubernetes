using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace EventStoreProxy.Authentication;

public static class ServiceCollectionExtensions
{
    public static void AddCertificateAuthentication(this IServiceCollection services, IConfigurationSection config)
    {
        services.AddSingleton<CertificateService>()
            .AddHostedService<CertificatesWorker>();

        services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.All;
                options.Events = new CertificateAuthenticationEvents()
                {
                    OnCertificateValidated = context => context.HttpContext.RequestServices
                        .GetRequiredService<CertificateService>()
                        .OnCertificateValidated(context)
                };
            })
            .AddCertificateCache(config.Bind);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuth", new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
        });
        
        services.Configure<KestrelServerOptions>(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.AllowAnyClientCertificate();
            });
        });
    }

    public static void UseESTrustedAuth(this TransformBuilderContext builder)
    {
        //Works as follows (see appsettings.json)
        //1. Authorization header and 'Cookie' header are always removed
        //2. es-cred cookie is set to { "credentials": "unknown:" } downstream
        
        //Add ES-TrustedAuth header based on certificate
        builder.AddRequestTransform(transformContext =>
        {
            const string name = "ES-TrustedAuth";
            transformContext.ProxyRequest.Headers.Remove(name);
            transformContext.ProxyRequest.Headers.Add(name,
                transformContext.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value);
            return ValueTask.CompletedTask;
        });
    }

    public static WebApplication UseAuthEndpoint(this WebApplication app)
    {
        app.MapGet("/Me", async context =>
        {
            var certificate = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var auth = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (auth == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
                return;
            }
            var user = CertificateService.User.Parse(auth);
            await context.Response.WriteAsJsonAsync(new {certificate, user.Username, user.Groups});
        });
        return app;
    }
}