
using ClientCertificatesCli;
using ClientCertificatesCli.Verbs;
using CommandLine;
using k8s;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

return Parser.Default.ParseArguments<Create, Get, List, Update, Delete>(args)
    .MapResult((Create o) => Handle(o, o.Handle),
        (Get o) => Handle(o, o.Handle),
        (List o) => Handle(o, o.Handle),
        (Update o) => Handle(o, o.Handle),
        (Delete o) => Handle(o, o.Handle),
        _ => 2);

static int Handle<T>(T options, Action<CertificatesService> @delegate)
    where T : VerbBase
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate: "[{Level:u3}]: {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
    try
    {
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        var service = new CertificatesService(config, options.Namespace, options.Secret);
        @delegate(service);
        return 0;
    }
    catch (Exception e)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        Log.Fatal(e, e.Message);
        return 1;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}