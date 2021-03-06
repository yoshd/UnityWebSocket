# UnityWebSocket

UnityWebSocket is a client library that allows transparent use of WebSocket on both WebGL and other platforms.

Unity WebGL has the limitation that the browser cannot handle the socket API directly, so WebSocket must be used from Javascript via Emscripten. (See [Documentation](https://docs.unity3d.com/2021.3/Documentation/Manual/webgl-networking.html))

## Requirements

- Unity 2021.3 or later
- It also assumes a dependency on [UniTask](https://github.com/Cysharp/UniTask) .

## Usage

You can add `"com.yoshd.unitywebsocket": "https://github.com/yoshd/UnityWebSocket.git#v0.1.4"` to your `manifest.json` .

Messages are always sent in binary type.

```cs
using UnityWebSocket;

async UniTask SampleAsync()
{
    var client = new WebSocketClient();
    client.ConnectAsync(new Uri("ws://localhost:8080/hello"), CancellationToken.None);
    var msg = System.Text.Encoding.UTF8.GetBytes("Hello!");
    await client.SendAsync(msg.AsMemory(), CancellationToken.None);
    var buf = new Memory<byte>(new byte[1024]);
    var r = await client.ReceiveAsync(buf, CancellationToken.None);
    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
    client.Dispose();
}
```
