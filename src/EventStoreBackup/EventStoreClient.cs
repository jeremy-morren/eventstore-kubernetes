using System.Net.Http.Headers;
using k8s.Models;

namespace EventStoreBackup;

public class EventStoreClient
{
    private readonly HttpClient _client;

    public EventStoreClient(HttpClient client) => _client = client;

    /// <summary>
    /// Sends a GET request to an EventStore pod
    /// </summary>
    public Task<HttpResponseMessage> GetAsync(V1Pod pod, 
        string requestUri, 
        AuthenticationHeaderValue? authentication,
        CancellationToken ct)
    {
        var @base = new Uri($"https://{pod.Name()}.{pod.Namespace()}.svc.cluster.local:2113/", UriKind.Absolute);
        var message = new HttpRequestMessage()
        {
            RequestUri = new Uri(@base, requestUri),
            Method = HttpMethod.Get,
            Headers =
            {
                Authorization = authentication,
                Accept = {new MediaTypeWithQualityHeaderValue("application/json")}
            },
        };
        return _client.SendAsync(message, ct);
    }
}