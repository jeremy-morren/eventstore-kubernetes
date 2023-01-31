using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

// ReSharper disable CollectionNeverUpdated.Global

namespace EventStoreBackup;

public class EventStoreOptions
{
    public string Namespace { get; set; } = null!;

    /// <summary>
    /// Map of HTTP domain names to EventStore Pods
    /// </summary>
    public Dictionary<string, string> Pods { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Namespace))
            throw new InvalidOperationException("Namespace is required");
        if (Pods.Count == 0)
            throw new InvalidOperationException("At least 1 Host map is required");
    }
}

public static class HttpContextExtensions
{
    public static Task<V1Pod> GetPodAsync(this HttpContext context, CancellationToken ct)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<EventStoreOptions>>().Value;
        var service = context.RequestServices.GetRequiredService<Kubernetes>();

        var podName = options.Pods[context.Request.Host.ToString()];
        return service.ReadNamespacedPodAsync(podName, options.Namespace, false, ct);
    }
}