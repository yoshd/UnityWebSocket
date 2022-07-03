using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnityWebSocket
{
    internal class WebSocketClientWebGL : IWebSocketClient
    {
        private static int _nextInstanceId = 0;

        private int _instanceId;

        // The message not passed by ReceiveAsync is temporarily retained and passed the next time ReceiveAsync is called.
        private byte[] _prevMsgBuffer = null;
        private int _prevMsgNextIndex = 0;
        private WebSocketMessageType _prevMsgType = default;

        private bool _disposed = false;

        public WebSocketState State { get; private set; } = WebSocketState.None;

        public WebSocketClientWebGL()
        {
            _instanceId = _nextInstanceId;
            _nextInstanceId++;
            _instances[_instanceId] = this;
        }

        public async UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            State = WebSocketState.Connecting;
            Jslib_InitializeWebSocket(_instanceId, OnOpenFunc, OnMessageFunc, OnErrorFunc, OnCloseFunc);
            Jslib_ConnectWebSocket(_instanceId, uri.ToString());
            do
            {
                await UniTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            } while (State is WebSocketState.Connecting);
        }

        public UniTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            unsafe
            {
                fixed (byte* data = &MemoryMarshal.GetReference(buffer.Span))
                {
                    Jslib_SendWebSocketMessage(_instanceId, data, buffer.Length);
                }
            }

            return UniTask.CompletedTask;
        }

        public UniTask SendAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return SendAsync(buffer.AsMemory(), cancellationToken);
        }

        public async UniTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (_prevMsgBuffer is null)
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_receivedMessageQueue.TryDequeue(out var msg))
                    {
                        if (msg.Item2.Length <= buffer.Length)
                        {
                            msg.Item2.CopyTo(buffer);
                            return new ValueWebSocketReceiveResult(msg.Item2.Length, msg.Item1, true);
                        }

                        _prevMsgBuffer = msg.Item2;
                        _prevMsgNextIndex = 0;
                        _prevMsgType = msg.Item1;
                        break;
                    }

                    await UniTask.Yield();
                }
            }

            // Cannot Marshal.Copy(msg.Item2, buffer, 0, buffer.Length);
            var writtenLength = 0;
            var currentIndex = _prevMsgNextIndex;
            var complete = false;
            for (var i = 0; i < buffer.Length; i++)
            {
                currentIndex += i;

                buffer.Span[i] = _prevMsgBuffer[currentIndex];
                writtenLength++;
                complete = _prevMsgBuffer.Length <= currentIndex;
                if (complete)
                {
                    _prevMsgBuffer = null;
                    _prevMsgNextIndex = 0;
                    break;
                }
            }

            return new ValueWebSocketReceiveResult(writtenLength, _prevMsgType, complete);
        }

        public async UniTask<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            var result = await ReceiveAsync(buffer.AsMemory(), cancellationToken);
            return new WebSocketReceiveResult(result.Count, result.MessageType, result.EndOfMessage);
        }

        public UniTask CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            Jslib_CloseWebSocket(_instanceId, (int)closeStatus, statusDescription);
            State = WebSocketState.CloseSent;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (State is WebSocketState.Open or WebSocketState.Connecting)
            {
                Jslib_CloseWebSocket(_instanceId, 1000, "Disposed");
            }

            State = WebSocketState.None;
            _instances.Remove(_instanceId);
            Jslib_DisposeWebSocket(_instanceId);
        }

        private readonly Queue<(WebSocketMessageType, byte[])> _receivedMessageQueue = new();

        private static readonly Dictionary<int, WebSocketClientWebGL> _instances = new();

        private delegate void OnOpen(int instanceId);

        private delegate void OnMessage(int instanceId, int webSocketMessageType, IntPtr ptr, int size);

        private delegate void OnError(int instanceId);

        private delegate void OnClose(int instanceId, int code);

        [MonoPInvokeCallback(typeof(OnOpen))]
        private static void OnOpenFunc(int instanceId)
        {
            _instances[instanceId].State = WebSocketState.Open;
        }

        [MonoPInvokeCallback(typeof(OnMessage))]
        private static void OnMessageFunc(int instanceId, int webSocketMessageType, IntPtr ptr, int size)
        {
            // TODO: Use MemoryPool<T> for efficiency.
            var msg = new byte[size];
            Marshal.Copy(ptr, msg, 0, size);
            _instances[instanceId]._receivedMessageQueue.Enqueue(((WebSocketMessageType)webSocketMessageType, msg));
        }

        [MonoPInvokeCallback(typeof(OnError))]
        private static void OnErrorFunc(int instanceId)
        {
            _instances[instanceId].State = WebSocketState.Aborted;
        }

        [MonoPInvokeCallback(typeof(OnClose))]
        private static void OnCloseFunc(int instanceId, int code)
        {
            // FYI: https://developer.mozilla.org/ja/docs/Web/API/CloseEvent/code
            if (!_instances[instanceId]._disposed)
            {
                _instances[instanceId].State = code == 1000 ? WebSocketState.Closed : WebSocketState.Aborted;
            }
        }

        [DllImport("__Internal")]
        private static extern void Jslib_InitializeWebSocket(int instanceId, OnOpen onOpen,
            OnMessage onMessage, OnError onError, OnClose onClose);

        [DllImport("__Internal")]
        private static extern void Jslib_ConnectWebSocket(int instanceId, string url);

        [DllImport("__Internal")]
        private static extern unsafe void Jslib_SendWebSocketMessage(int instanceId, byte* message, int length);

        [DllImport("__Internal")]
        private static extern void Jslib_CloseWebSocket(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        private static extern void Jslib_DisposeWebSocket(int instanceId);
    }
}