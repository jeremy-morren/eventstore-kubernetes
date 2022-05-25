using System;
using System.IO;
using EventStore.Setup.Verbs;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EventStore.Setup.Tests;

public class GenOptionsTests
{
    [Theory]
    [InlineData(3, "esdb-cluster", "esdb-cluster-1", 2114, "1.2.3.4")]
    [InlineData(5, "eventstore-abc", "eventstore-abc-2", 43584, "4.3.2.1")]
    [InlineData(5, "eventstore", "eventstore-2", null, "4.3.2.1")]
    public void GenYaml(int clusterSize, string clusterName, string hostname, int? gossipPort, string podIP)
    {
        var svc = new GenOptions(clusterSize, hostname, gossipPort?.ToString(), podIP);
        var sw = new StringWriter();
        svc.Process(sw);
        var options = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build()
            .Deserialize<YamlOptions>(sw.ToString());
        Assert.Equal(clusterSize, options.ClusterSize);
        var seeds = options.GossipSeed.Split(",");
        Assert.Equal(clusterSize - 1, seeds.Length);
        Assert.All(seeds, s =>
        {
            Assert.DoesNotContain(hostname, s);
            Assert.StartsWith(clusterName, s);
            var port = gossipPort ?? 2113;
            Assert.EndsWith($":{port}", s);
        });
        Assert.False(options.DiscoverViaDns);
        Assert.Equal(podIP, options.IntHostAdvertiseAs);
        Assert.Equal(podIP, options.IntIp);
    }
    
    [Theory]
    [InlineData("cluster-a", 3)]
    [InlineData("cluster", 3)]
    [InlineData("cluster-3", 3)]
    public void InvalidHostnameShouldFail(string hostname, int clusterSize)
    {
        var options = new GenOptions(clusterSize, hostname, "2113", "1.2.3.4");
        Assert.Throws<ArgumentException>(() => options.Process(new StringWriter()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(ushort.MaxValue + 1)]
    public void InvalidGossipPortShouldFail(int port)
    {
        var options = new GenOptions(3, "na-1", port.ToString(), "1.2.3.4");
        Assert.Throws<ArgumentException>(() => options.Process(new StringWriter()));
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void InvalidClusterSizeShouldFail(int clusterSize)
    {
        var options = new GenOptions(clusterSize, "na-1",  "2113", "1.2.3.4");
        Assert.Throws<ArgumentException>(() => options.Process(new StringWriter()));
    }
}