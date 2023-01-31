using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using k8s;
using k8s.Models;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace EventStoreBackup.K8s;

public class K8sExec
{
    private readonly Kubernetes _client;
    private readonly ILogger<K8sExec> _logger;

    public K8sExec(Kubernetes client,
        ILogger<K8sExec> logger)
    {
        _client = client;
        _logger = logger;
    }

    private K8sExecResponse HandleResponse(V1Pod pod, 
        IReadOnlyList<string> cmd, 
        WebSocket webSocket,
        K8sExecResponse response)
    {
        switch (response.Status)
        { 
            case K8sExecStatus.Success:
                return response;
            default:
                _logger.LogWarning("Error executing command {Command} in pod {@Pod}. Websocket: {@Websocket}. Response: {@Response}", 
                    FormatCommand(cmd), 
                    pod,
                    new
                    {
                        webSocket.State,
                        CloseStatus = webSocket.CloseStatus != null ? $"{webSocket.CloseStatus}:{webSocket.CloseStatusDescription}" : null
                    },
                    response);
                throw new K8sExecException(FormatCommand(cmd), response);
        }
    }

    public Task<K8sExecResponse> Exec(V1Pod pod, IEnumerable<string> command, CancellationToken ct) =>
        Exec(pod, pod.Spec.Containers[0].Name, command, ct);
    
    public async Task<K8sExecResponse> Exec(V1Pod pod, 
        string container,
        IEnumerable<string> command, 
        CancellationToken ct)
    {
        var cmd = command.ToList();
        try
        {
            _logger.LogInformation("Executing command {Command} on pod {@Pod}", FormatCommand(cmd), pod);
        
            using var websocket = await _client.WebSocketNamespacedPodExecAsync(name: pod.Name(), 
                @namespace: pod.Namespace(), 
                command: cmd,
                container: container,
                stdin: false,
                stdout: true,
                stderr: true,
                tty: false,
                webSocketSubProtol: WebSocketProtocol.V4BinaryWebsocketProtocol,
                cancellationToken: ct);

            var stdOut = new StringBuilder();
            var response = await ReadResponse(new WebSocketReader(websocket),
                (_, buffer, offset, count) =>
                {
                    stdOut.Append(Utf8Encoding.GetString(buffer, offset, count));
                    return ValueTask.CompletedTask;
                }, ct);

            response.StdOut = stdOut.Length > 0 ? stdOut.ToString() : null;

            return HandleResponse(pod, cmd, websocket, response);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error executing command {Command} in pod {@Pod}", 
                FormatCommand(cmd), pod);
            throw;
        }
    }

    public Task<K8sExecResponse> Exec(V1Pod pod, IEnumerable<string> command, PipeWriter pipeOut, CancellationToken ct) => 
        Exec(pod, pod.Spec.Containers[0].Name, command, pipeOut, ct);

    public async Task<K8sExecResponse> Exec(V1Pod pod, 
        string container, 
        IEnumerable<string> command,
        PipeWriter pipeOut, 
        CancellationToken ct)
    {
        var cmd = command.ToList();
        try
        {
            _logger.LogInformation("Executing command {Command} on pod {@Pod}", FormatCommand(cmd), pod);
        
            using var websocket = await _client.WebSocketNamespacedPodExecAsync(name: pod.Name(), 
                @namespace: pod.Namespace(), 
                command: cmd,
                container: container,
                stdin: false,
                stdout: true,
                stderr: true,
                tty: false,
                webSocketSubProtol: WebSocketProtocol.V4BinaryWebsocketProtocol,
                cancellationToken: ct);

            var response = await ReadResponse(new WebSocketReader(websocket),
                async (_, buffer, offset, count) =>
                {
                    var result = await pipeOut.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), ct);
                    if (result.IsCanceled)
                        throw new OperationCanceledException("Pipe flush cancelled");
                },
                ct);
            
            return HandleResponse(pod, cmd, websocket, response);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error executing command {Command} in pod {@Pod}", 
                FormatCommand(cmd), pod);
            throw;
        }
    }

    private static async Task<K8sExecResponse> ReadResponse(WebSocketReader reader,
        OnWebSocketDataReceived stdOutHandler, 
        CancellationToken ct)
    {
        var control = new StringBuilder();
        var stdErr = new StringBuilder();
        await reader.Read(async (channelIndex, buffer, offset, count) =>
            {
                switch (channelIndex)
                {
                    case ChannelIndex.Error:
                        control.Append(Utf8Encoding.GetString(buffer, offset, count));
                        break;
                    case ChannelIndex.StdErr:
                        stdErr.Append(Utf8Encoding.GetString(buffer, offset, count));
                        break;
                    case ChannelIndex.StdOut:
                        await stdOutHandler(channelIndex, buffer, offset, count);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown channel '{channelIndex}'");
                }
            },
            4 * 1024 * 1024, //4 KB
            ct);

        var response = control.Length > 0
            ? JsonSerializer.Deserialize<K8sExecResponse>(control.ToString(), K8sExecResponse.JsonOptions) 
              ?? throw new InvalidOperationException("Control response is null") 
            : new K8sExecResponse()
            {
                Status = K8sExecStatus.Unknown
            };

        response.StdErr = stdErr.Length > 0 ? stdErr.ToString() : null;
        
        return response;
    }
    
    private static string FormatCommand(IEnumerable<string> cmd)
    {
        var list = cmd.Select(a =>
        {
            if (!a.Contains(' ') && !a.Contains('"'))
                return a;
            a = a.Replace("\"", "\\\""); // " -> \"
            return $"\"{a}\"";
        });
        return string.Join(" ", list);
    }

    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
}