using System.Buffers;
using System.Net.WebSockets;
using k8s;

namespace EventStoreBackup.K8s;

/// <summary>
/// Reads a Kubernetes Exec websocket response,Based on <see cref="k8s.StreamDemuxer"/>,
/// tested with <see cref="k8s.WebSocketProtocol.V4BinaryWebsocketProtocol"/>
/// </summary>
/// <remarks>
/// Based on <see cref="k8s.StreamDemuxer"/>, tested with <see cref="k8s.WebSocketProtocol.V4BinaryWebsocketProtocol"/>
/// </remarks>
public class WebSocketReader
{
    private readonly WebSocket _webSocket;
    private readonly StreamType _streamType;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamDemuxer"/> class.
    /// </summary>
    /// <param name="webSocket">
    /// A <see cref="WebSocket"/> which contains a multiplexed stream, such as the <see cref="WebSocket"/> returned by the exec or attach commands.
    /// </param>
    /// <param name="streamType">
    /// A <see cref="StreamType"/> specifies the type of the stream.
    /// </param>
    public WebSocketReader(WebSocket webSocket, StreamType streamType = StreamType.RemoteCommand)
    {
        _streamType = streamType;
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
    }

    public Task Read(OnWebSocketDataReceived handler, CancellationToken cancellationToken) => 
        Read(handler, 1024 * 1024, cancellationToken);  //1KB buffer
        
    public async Task Read(OnWebSocketDataReceived handler, 
        int bufferSize,
        CancellationToken cancellationToken)
    {
        // Get a read buffer
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        // This set tracks whether we have skipped any bytes from a stream
        var streamBytesToSkipMap = new HashSet<byte>();
        try
        {
            var segment = new ArraySegment<byte>(buffer);

            while (!cancellationToken.IsCancellationRequested && _webSocket.CloseStatus == null)
            {
                // We always get data in this format:
                // [stream index] (1 for stdout, 2 for stderr)
                // [payload]
                var result = await _webSocket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
                
                // Ignore empty messages
                if (result.Count < 2)
                    continue;

                var streamIndex = buffer[0];
                var extraByteCount = 1;

                //Read the payload
                while (true)
                {
                    int bytesToSkip;
                    if (!streamBytesToSkipMap.Contains(streamIndex))
                        // When used in port-forwarding, the first 2 bytes from the web socket on all channels is port bytes, skip.
                        // https://github.com/kubernetes/kubernetes/blob/master/pkg/kubelet/cri/streaming/portforward/websocket.go
                        // https://github.com/kubernetes/kubernetes/blob/3aff1f97bef13f1d40e47fae785994d57c3e5a80/pkg/kubelet/cri/streaming/portforward/websocket.go#L132-L136
                        bytesToSkip = _streamType == StreamType.PortForward ? 2 : 0;
                    else
                        bytesToSkip = 0;

                    var bytesCount = result.Count - extraByteCount;
                    if (bytesToSkip <= bytesCount) // Otherwise skip the entire data.
                    {
                        bytesCount -= bytesToSkip;
                        extraByteCount += bytesToSkip;
                        
                        await handler((ChannelIndex)streamIndex, buffer, extraByteCount, bytesCount);
                    }

                    streamBytesToSkipMap.Add(streamIndex);

                    if (result.EndOfMessage)
                        break;
                    
                    extraByteCount = 0;
                    result = await _webSocket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

public delegate ValueTask OnWebSocketDataReceived(ChannelIndex channelIndex, byte[] buffer, int offset, int count);