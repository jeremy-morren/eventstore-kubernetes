using System.Diagnostics.CodeAnalysis;

// ReSharper disable InconsistentNaming

namespace EventStoreBackup.K8s;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class K8sExecException : Exception
{
    public string Command { get; }
    public K8sExecResponse Response { get; }

    public K8sExecException(string command, K8sExecResponse response)
        : base($"Error running command '{command}': {response.Status}: {FormatMessage(response.Message)}: {FormatMessage(response.StdErr)}")
    {
        Command = command;
        Response = response;
    }

    public static string? FormatMessage(string? input) =>
        input?.ReplaceLineEndings()
            .Replace(Environment.NewLine, "\\n");
}