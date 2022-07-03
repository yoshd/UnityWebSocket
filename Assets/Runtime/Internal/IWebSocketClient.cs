using System;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UnityWebSocket
{
    internal interface IWebSocketClient : IDisposable
    {
        UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken);

        UniTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        UniTask SendAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        UniTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        UniTask CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken);
    }
}