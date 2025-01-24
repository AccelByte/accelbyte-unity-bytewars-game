#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using WebSocketSharp;
#endif

namespace Netcode.Transports.WebSocket
{
    public class WebSocketClientFactory
    {
#if (UNITY_WEBGL && !UNITY_EDITOR)
        public static JSWebSocketClient Client;

        internal delegate void OnOpenCallback();
        internal delegate void OnMessageCallback(IntPtr messagePointer, int messageSize);
        internal delegate void OnErrorCallback(IntPtr errorPointer);
        internal delegate void OnCloseCallback(int closeCode);

        [DllImport("__Internal")]
        internal static extern void _SetUrl(string url);
        [DllImport("__Internal")]
        internal static extern void _SetOnOpen(OnOpenCallback callback);
        [DllImport("__Internal")]
        internal static extern void _SetOnMessage(OnMessageCallback callback);
        [DllImport("__Internal")]
        internal static extern void _SetOnError(OnErrorCallback callback);
        [DllImport("__Internal")]
        internal static extern void _SetOnClose(OnCloseCallback callback);

        [MonoPInvokeCallback(typeof(OnOpenCallback))]
        internal static void OnOpenEvent()
        {
            Client.OnOpen();
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        internal static void OnMessageEvent(IntPtr payloadPointer, int length)
        {
            var buffer = new byte[length];

            Marshal.Copy(payloadPointer, buffer, 0, length);
            Client.OnMessage(new ArraySegment<byte>(buffer, 0, length));
        }

        [MonoPInvokeCallback(typeof(OnErrorCallback))]
        internal static void OnErrorEvent(IntPtr errorPointer)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPointer);
            Client.OnError(errorMessage);
        }

        [MonoPInvokeCallback(typeof(OnCloseCallback))]
        internal static void OnCloseEvent(int disconnectCode)
        {
            CloseStatusCode code = (CloseStatusCode)disconnectCode;

            if (!Enum.IsDefined(typeof(CloseStatusCode), code))
            {
                code = CloseStatusCode.Undefined;
            }

            Client.OnClose(code);
        }
#endif

        public static IWebSocketClient Create(bool useSecureConnection, string url, string username, string password)
        {
            string protocol = useSecureConnection ? "wss" : "ws";
            string targetUrl = $"{protocol}://{url}";

#if (UNITY_WEBGL && !UNITY_EDITOR)
            // Reformat url to add basic authentication to WebSocket header.
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                targetUrl = $"{protocol}://{username}:{password}@{url}";
            }

            Client = new JSWebSocketClient();
            _SetUrl(targetUrl);
            _SetOnOpen(OnOpenEvent);
            _SetOnMessage(OnMessageEvent);
            _SetOnError(OnErrorEvent);
            _SetOnClose(OnCloseEvent);

            return Client;
#else
            return new NativeWebSocketClient(targetUrl, username, password);
#endif
        }
    }
}
