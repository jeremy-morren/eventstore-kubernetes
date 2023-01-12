using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit.Abstractions;

namespace EventStoreBackup.Tests;

public class WebApp : WebApplicationFactory<Program>
{
    private readonly ITestOutputHelper? _output;
    
    public WebApp() : this (null) {}

    public WebApp(ITestOutputHelper? output) => _output = output;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        
//Use of obsolete symbol
#pragma warning disable CS0618
        builder.UseSerilog((context, conf) =>
        {
            conf.ReadFrom.Configuration(context.Configuration)
                .DestructureObjects();
            
            if (_output != null)
                conf.WriteTo.Sink(new TestOutputHelperSink(_output));
            else
                conf.WriteTo.Console();
        }, preserveStaticLogger: true, writeToProviders: false);
#pragma warning restore CS0618
        
        //Allow anonymous auth
        builder.ConfigureServices(services => 
            services.Configure<AuthorizationOptions>(o =>
            {
                //Allow anonymous
                o.FallbackPolicy = null;
            }));
    }
    
    
    private class TestOutputHelperSink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formatter;
        private readonly ITestOutputHelper _output;

        public TestOutputHelperSink(ITestOutputHelper output,
            string format = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        {
            _output = output;
            _formatter = new MessageTemplateTextFormatter(format);
        }

        public void Emit(LogEvent logEvent)
        {
            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            _output.WriteLine(writer.ToString().Trim());
        }
    }
}