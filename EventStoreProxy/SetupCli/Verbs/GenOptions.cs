using System.Text.RegularExpressions;
using CommandLine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ReSharper disable MemberCanBePrivate.Global

namespace EventStore.Setup.Verbs;

[Verb("gen-options", HelpText = "Generate options YAML for EventStore Kubernetes node")]
public class GenOptions
{
    public GenOptions(int clusterSize, 
        string hostname, 
        string? gossipPort, 
        string podIp)
    {
        ClusterSize = clusterSize;
        Hostname = hostname;
        GossipPort = gossipPort;
        PodIp = podIp;
    }

    [Option("cluster-size", Required = true, HelpText = ClusterSizeHelpText)]
    public int ClusterSize { get; }
    
    [Option("hostname", Required = true, HelpText = HostnameHelpText)]
    public string Hostname { get; }
    
    [Option("gossip-port", Required = false, HelpText = GossipPortHelpText)]
    public string? GossipPort { get; }

    [Option("pod-ip", Required = true, HelpText = PodIpHelpText)]
    public string PodIp { get; }
    
    public void Process(TextWriter outWriter)
    {
        static void Validate(string str, string option)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException($"{option} is required");
        }
        Validate(Hostname, "--hostname");
        Validate(PodIp, "--pod-ip");
        
        if (ClusterSize <= 0)
            throw new ArgumentException("Cluster size must be greater than 0");
        var gossipPort = string.IsNullOrWhiteSpace(GossipPort) ? 2113
            : int.TryParse(GossipPort, out var p) ? p
            : throw new ArgumentException($"Could not parse gossip port '{GossipPort}'");
        if (gossipPort is <= 0 or > ushort.MaxValue)
            throw new ArgumentException($"Gossip port must be greater than 0 and less or equal to {ushort.MaxValue}");
        //Hostname should be of the format {ClusterName}-{Number}
        var match = Regex.Match(Hostname, "^(?<name>.+)-(?<num>[0-9]+)$");
        if (!match.Success || !int.TryParse(match.Groups["num"].Value, out var num))
            throw new ArgumentException(
                $"Invalid hostname '{Hostname}'. Must match regex (?<=^.+-)[0-9]+$");
        if (num >= ClusterSize)
            throw new ArgumentException($"Invalid node id {num}. Must be less than ClusterSize ({ClusterSize})");
        if (num < 0)
            throw new ArgumentException($"Invalid node id {num}. Must be greater than or equal to 0");
        var nodes = Enumerable.Range(0, ClusterSize)
            .Where(i => i != num)
            .Select(i => $"{match.Groups["name"]}-{i}:{gossipPort}");
        var options = new Dictionary<string, object>()
        {
            {"IntHostAdvertiseAs", PodIp},
            {"IntIp", PodIp},
            
            {"GossipSeed", string.Join(",", nodes)},
            {"DiscoverViaDns", false}
        };
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        serializer.Serialize(outWriter, options);
    }

    #region HelpText

    private const string HostnameHelpText =
        "The hostname (identifier) of this node.  Will be assigned to the 'ExtHostAdvertiseAs' option as '{Name}.{Namespace}.svc.cluster.local'";

    private const string PodIpHelpText =
        "The IP Address of the pod that the node is deployed in.  It will be assigned to the 'IntHostAdvertiseAs, IntIP, ExtIP' options";

    private const string ClusterSizeHelpText =
        "The size of the EventStore cluster. Will be used to generate the Gossip seeds";

    private const string GossipPortHelpText =
        "The port number used for internal gossip. Default 2113";

    #endregion

}