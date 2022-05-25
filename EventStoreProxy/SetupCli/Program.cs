
using CommandLine;
using EventStore.Setup;
using EventStore.Setup.Verbs;
using Serilog;

return Parser.Default.ParseArguments<GenOptions, CombineOptions, CreateCa, CreateNode>(args)
    .MapResult((GenOptions o) => Process(() => o.Process(Console.Out)),
        (CombineOptions o) => Process(() => o.Combine(Console.Out)),
        (CreateCa o) => Process(o.Process),
        (CreateNode o) => Process(o.Process),
        _ => 2);

static int Process(Action process)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();
    try
    {
        process();
        return 0;
    }
    catch (Exception e)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        Log.Fatal(e, e.Message);
        return e is ArgumentException ? 2 : 1; //return 2 for bad arguments
    }
    finally
    {
        Log.CloseAndFlush();
    }
}