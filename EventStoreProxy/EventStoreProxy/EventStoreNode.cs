using System.Diagnostics.Contracts;

namespace EventStoreProxy;

public class EventStoreNode
{
    private string _internalHost = null!;
    private string _publicHost = null!;

    public string PublicHost
    {
        get => _publicHost;
        set => _publicHost = TrimTrailingSlash(value);
    }

    public string InternalHost
    {
        get => _internalHost;
        set => _internalHost = TrimTrailingSlash(value);
    }

    [Pure]
    private static string TrimTrailingSlash(string value) => value.EndsWith("/") ? value[..^1] : value;
}