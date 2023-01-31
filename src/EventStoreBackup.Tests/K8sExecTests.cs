using System.IO.Pipelines;
using EventStoreBackup.K8s;
using k8s;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable InconsistentNaming

namespace EventStoreBackup.Tests;

public class K8sExecTests
{
    [Fact]
    public async Task Exec_Should_Return_StdOut()
    {
        var ct = new CancellationTokenSource(10000).Token;
        var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        var pod = await client.ReadNamespacedPodAsync(Pod2, Namespace, false, ct);
        var exec = new K8sExec(client, NullLogger<K8sExec>.Instance);

        var response = await exec.Exec(pod, new[] {"ls", "/data/db", "-l"}, ct);
        Assert.Equal(K8sExecStatus.Success, response.Status);
        Assert.Null(response.StdErr);
        Assert.NotNull(response.StdOut);
    }
    
    [Fact]
    public async Task Exec_With_PipeOut_Should_Write_Output()
    {
        var ct = new CancellationTokenSource(10000).Token;
        var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        var pod = await client.ReadNamespacedPodAsync(Pod1, Namespace, false, ct);
        var exec = new K8sExec(client, NullLogger<K8sExec>.Instance);

        var output = new MemoryStream();
        var writer = PipeWriter.Create(output);
        var response = await exec.Exec(pod, new[] {"ls", "/data/db", "-l"}, writer, ct);
        await writer.FlushAsync(ct);
        
        Assert.Equal(K8sExecStatus.Success, response.Status);
        Assert.Null(response.StdErr);
        Assert.Null(response.StdOut);

        Assert.NotEqual(0, output.Length);
    }

    [Fact]
    public async Task Exec_Failure_Should_Return_StdErr()
    {
        var ct = new CancellationTokenSource(10000).Token;
        var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        var pod = await client.ReadNamespacedPodAsync(Pod0, Namespace, false, ct);
        var exec = new K8sExec(client, NullLogger<K8sExec>.Instance);
        
        var ex = await Assert.ThrowsAsync<K8sExecException>(() => exec.Exec(pod, new[] {"ls", "/asdf"}, ct));

        Assert.NotNull(ex.Response);
        Assert.Equal(K8sExecStatus.Failure, ex.Response.Status);
        Assert.NotNull(ex.Response.StdErr);
        Assert.Null(ex.Response.StdOut);
    }

    private const string Namespace = "db";

    private const string Pod0 = "esdb-0";
    private const string Pod1 = "esdb-1";
    private const string Pod2 = "esdb-2";
}