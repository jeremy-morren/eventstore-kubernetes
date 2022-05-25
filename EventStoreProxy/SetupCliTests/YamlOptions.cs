// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS8618

namespace EventStore.Setup.Tests;

public record YamlOptions
{
    public int ClusterSize { get; set; }
    public bool DiscoverViaDns { get; set; } = true;
    public string IntHostAdvertiseAs { get; set; }
    public string GossipSeed { get; set; }
    public string IntIp { get; set; }
}