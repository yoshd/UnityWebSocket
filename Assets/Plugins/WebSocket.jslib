var WebSocketLibrary = {
    $instances: {},

    Jslib_InitializeWebSocket: function (instanceId, onOpen, onMessage, onError, onClose) {
        instances[instanceId] = {
            id: instanceId, socket: null, onOpen: onOpen, onMessage: onMessage, onError: onError, onClose: onClose,
        };
    },

    Jslib_ConnectWebSocket: function (instanceId, url) {
        var instance = instances[instanceId];
        var urlStr = UTF8ToString(url);
        instance.socket = new WebSocket(urlStr);
        instance.socket.onopen = function (e) {
            dynCall('vi', instance.onOpen, [instance.id]);
        };

        instance.socket.onmessage = function (e) {
            if (e.data instanceof Blob) {
                var reader = new FileReader();
                reader.addEventListener("loadend", function () {
                    var data = new Uint8Array(reader.result);
                    var buffer = _malloc(data.length);
                    HEAPU8.set(data, buffer);
                    try {
                        dynCall('viiii', instance.onMessage, [instance.id, 1, buffer, data.length]);
                    } finally {
                        _free(buffer);
                    }
                });
                reader.readAsArrayBuffer(e.data);
            } else if (e.data instanceof ArrayBuffer) {
                var data = new Uint8Array(e.data);
                var buffer = _malloc(data.length);
                HEAPU8.set(data, buffer);
                try {
                    dynCall('viiii', instance.onMessage, [instance.id, 1, buffer, data.length]);
                } finally {
                    _free(buffer);
                }
            } else {
                // Text
                var length = lengthBytesUTF8(e.data) + 1;
                var buffer = _malloc(length);
                stringToUTF8(e.data, buffer, length)
                try {
                    dynCall('viiii', instance.onMessage, [instance.id, 0, buffer, length]);
                } finally {
                    _free(buffer);
                }
            }
        };

        instance.socket.onerror = function (e) {
            dynCall('vi', instance.onError, [instance.id]);
        }

        instance.socket.onclose = function (e) {
            dynCall('vii', instance.onClose, [instance.id, e.code]);
        };
    },

    Jslib_SendWebSocketMessage: function (instanceId, ptr, length) {
        instances[instanceId].socket.send(HEAP8.subarray(ptr, ptr + length));
    },

    Jslib_CloseWebSocket: function (instanceId, code, reason) {
        instances[instanceId].socket.close(code, UTF8ToString(reason));
    },

    Jslib_DisposeWebSocket: function (instanceId, code, reason) {
        delete instances[instanceId];
    }
}

autoAddDeps(WebSocketLibrary, '$instances');
mergeInto(LibraryManager.library, WebSocketLibrary);
