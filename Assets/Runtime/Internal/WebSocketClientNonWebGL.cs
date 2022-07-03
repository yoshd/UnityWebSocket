﻿using System;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UnityWebSocket
{
    internal class WebSocketClientNonWebGL : IWebSocketClient
    {
        private readonly ClientWebSocket _client = new ClientWebSocket();

        public async UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async UniTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            await _client.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }

        public async UniTask SendAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await _client.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }

        public async UniTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            return await _client.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public async UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            return await _client.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public async UniTask CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            await _client.CloseAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}