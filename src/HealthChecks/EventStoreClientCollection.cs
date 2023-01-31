using System.Collections;
using System.Collections.Concurrent;
using EventStore.Client;

namespace HealthChecks.EventStoreDB.Grpc;

/// <summary>
/// Tracks multiple instances of <see cref="EventStoreClient"/>,
/// and provides unified disposal
/// </summary>
/// <remarks>
/// For efficiency, <see cref="EventStoreClient"/> should be registered as a singleton.
/// This wrapper enables multiple singleton registrations of <see cref="EventStoreClient"/>,
/// and therefore multiple HealthChecks
/// </remarks>
internal class EventStoreClientCollection : IReadOnlyDictionary<string, EventStoreClient>, IDisposable, IAsyncDisposable
{
    //Because the number of items is expected to be low (usually 1), we can just lock on the dictionary
    private readonly Dictionary<string, EventStoreClient> _clients = new();

    public EventStoreClient GetOrAdd(string key, Func<EventStoreClient> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        lock (_clients)
        {
            if (_clients.TryGetValue(key, out var client)) return client;
            client = factory() ?? throw new InvalidOperationException("Factory returned null value");
            _clients.Add(key, client);
            return client;
        }
    }

    public ValueTask RemoveAsync(string key)
    {
        EventStoreClient client;
        lock (_clients)
        {
            if (!_clients.Remove(key, out client!))
                throw new KeyNotFoundException($"No client with key '{key}' found");
        }
        return client.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        List<EventStoreClient> clients;
        lock (_clients)
        {
            clients = _clients.Values.ToList();
            _clients.Clear();
        }
        foreach (var client in clients)
            await client.DisposeAsync();
    }
    
    public void Dispose()
    {
        List<EventStoreClient> clients;
        lock (_clients)
        {
            clients = _clients.Values.ToList();
            _clients.Clear();
        }
        foreach (var client in clients)
#if NET5_0_OR_GREATER
            client.Dispose();
#else
            client.DisposeAsync().AsTask().Wait();
#endif
    }

    #region Implementation of IReadOnlyDictionary
    
    public IEnumerator<KeyValuePair<string, EventStoreClient>> GetEnumerator() => _clients.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _clients).GetEnumerator();

    public int Count => _clients.Count;

    public bool ContainsKey(string key) => _clients.ContainsKey(key);

    public bool TryGetValue(string key, out EventStoreClient value) => _clients.TryGetValue(key, out value!);

    public EventStoreClient this[string key] => _clients[key];

    public IEnumerable<string> Keys => _clients.Keys;

    public IEnumerable<EventStoreClient> Values => _clients.Values;
    
    #endregion
}