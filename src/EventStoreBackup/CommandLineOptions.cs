using CommandLine;

namespace EventStoreBackup;

public readonly struct CommandLineOptions
{
    [Option("disable-auth", Default = false, HelpText = "Disable authentication")]
    public bool DisableAuth { get; }

    [Option("config", Required = false, HelpText = "Optional json configuration")]
    public string? Config { get; }

    public CommandLineOptions(bool disableAuth, string? config)
    {
        DisableAuth = disableAuth;
        Config = config;
    }
}