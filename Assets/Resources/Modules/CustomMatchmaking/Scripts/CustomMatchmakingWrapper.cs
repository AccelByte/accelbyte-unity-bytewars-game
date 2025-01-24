// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Net;
using Newtonsoft.Json;
using UnityEngine;
using NativeWebSocket;
#if UNITY_WEBGL
using Netcode.Transports.WebSocket;
using static Netcode.Transports.WebSocket.WebSocketEvent;
#endif

public class CustomMatchmakingWrapper : MonoBehaviour
{
    public Action OnMatchmakingStarted;
    public Action<WebSocketCloseCode /*closeCode*/> OnMatchmakingStopped;
    public Action<CustomMatchmakingModels.MatchmakerPayload /*payload*/> OnMatchmakingPayload;
    public Action<string /*serverIp*/, ushort /*serverPort*/> OnMatchmakingServerReady;
    public Action<string /*errorMessage*/> OnMatchmakingError;

#if !UNITY_WEBGL
    // Use native WebSocket.
    private WebSocket nativeWebSocket;
#else
    // Use Unity Netcode's WebSocket that supports WebGL.
    private IWebSocketClient netcodeWebSocket;
#endif

    public void StartMatchmaking() 
    {
        BytewarsLogger.Log("Start matchmaking.");

        string matchmakerUrl = CustomMatchmakingModels.GetMatchmakerUrl();
#if !UNITY_WEBGL 
        nativeWebSocket = new WebSocket(matchmakerUrl);
        nativeWebSocket.OnOpen += OnMatchmakerOpen;
        nativeWebSocket.OnClose += (int closeCode) => 
        {
            OnMatchmakerClosed(
                Enum.IsDefined(typeof(WebSocketCloseCode), closeCode) ?
                (WebSocketCloseCode)closeCode :
                WebSocketCloseCode.Undefined);
        };
        nativeWebSocket.OnMessage += (byte[] data) =>
        { 
            OnMatchmakerPayload(data);
        };
        nativeWebSocket.OnError += OnMatchmakerError;
        nativeWebSocket.Connect();
#else
        netcodeWebSocket = WebSocketClientFactory.Create(
            useSecureConnection: false, 
            url: matchmakerUrl, 
            username: string.Empty, 
            password: string.Empty);
        netcodeWebSocket.Connect();
#endif
    }

    public void CancelMatchmaking() 
    {
#if !UNITY_WEBGL
        if (nativeWebSocket == null)
        {
            BytewarsLogger.LogWarning("Cannot cancel matchmaking. WebSocket to matchmaker is null.");
            OnMatchmakerClosed(WebSocketCloseCode.Normal);
            return;
        }
#else
        if (netcodeWebSocket == null)
        {
            BytewarsLogger.LogWarning("Cannot cancel matchmaking. WebSocket to matchmaker is null.");
            OnMatchmakerClosed(WebSocketCloseCode.Normal);
            return;
        }
#endif

        BytewarsLogger.Log("Cancel matchmaking.");

#if !UNITY_WEBGL
        nativeWebSocket.Close();
#else
        netcodeWebSocket.Close();
#endif
    }

    private void Update()
    {
        PollMatchmakerEvent();
    }

    private void PollMatchmakerEvent() 
    {
#if !UNITY_WEBGL
        if (nativeWebSocket == null)
        {
            return;
        }

        nativeWebSocket.DispatchMessageQueue();
#else
        if (netcodeWebSocket == null)
        {
            return;
        }

        WebSocketEvent pollEvent = netcodeWebSocket.Poll();
        if (pollEvent != null)
        {
            switch (pollEvent.Type)
            {
                case WebSocketEventType.Open:
                    OnMatchmakerOpen();
                    break;
                case WebSocketEventType.Close:
                    OnMatchmakerClosed(
                        Enum.IsDefined(typeof(WebSocketCloseCode), (int)pollEvent.CloseCode) ? 
                        (WebSocketCloseCode)pollEvent.CloseCode : 
                        WebSocketCloseCode.Undefined);
                    break;
                case WebSocketEventType.Payload:
                    OnMatchmakerPayload(pollEvent.Payload);
                    break;
                case WebSocketEventType.Error:
                    OnMatchmakerError(pollEvent.Error);
                    break;
            }
        }
#endif
    }

    private void OnMatchmakerOpen() 
    {
        BytewarsLogger.Log("Connected to matchmaker.");
        OnMatchmakingStarted?.Invoke();
    }

    private void OnMatchmakerClosed(WebSocketCloseCode closeCode)
    {
        BytewarsLogger.Log($"Disconnected from matchmaker. Close code: {closeCode}");
#if !UNITY_WEBGL
        nativeWebSocket = null;
#else
        netcodeWebSocket = null;
#endif
        OnMatchmakingStopped?.Invoke(closeCode);
    }

    private void OnMatchmakerPayload(byte[] bytes) 
    {
        // Get the payload.
        string payloadStr = System.Text.Encoding.UTF8.GetString(bytes);
        if (string.IsNullOrEmpty(payloadStr))
        {
            BytewarsLogger.LogWarning("Cannot handle matchmaker payload. Payload is null.");
            return;
        }

        // Try parse the payload.
        CustomMatchmakingModels.MatchmakerPayload payload = null;
        try 
        {
            payload = JsonConvert.DeserializeObject<CustomMatchmakingModels.MatchmakerPayload>(payloadStr);
        }
        catch (Exception e) 
        {
            BytewarsLogger.LogWarning($"Cannot handle matchmaker payload. Unable to parse payload. Error: {e.Message}");
            return;
        }
        if (payload == null)
        {
            BytewarsLogger.LogWarning("Cannot handle matchmaker payload. Matchmaker payload is null.");
            return;
        }

        // Broadcast the payload.
        BytewarsLogger.Log($"Received payload from matchmaker: {payloadStr}");
        OnMatchmakingPayload?.Invoke(payload);

        // Travel to the server.
        if (payload.type == CustomMatchmakingModels.MatchmakerPayloadType.OnServerReady) 
        {
            OnMatchmakerServerReady(payload.message);
        }
    }

    private void OnMatchmakerError(string errorMessage) 
    {
        BytewarsLogger.Log($"Connection to matchmaker error. Error: {errorMessage}");
#if !UNITY_WEBGL
        nativeWebSocket = null;
#else
        netcodeWebSocket = null;
#endif
        OnMatchmakingError?.Invoke(errorMessage);
    }

    private void OnMatchmakerServerReady(string serverInfo) 
    {
        // Travel to the server if the server info is valid.
        if (TryParseServerInfo(serverInfo, out string serverIp, out ushort serverPort))
        {
            BytewarsLogger.Log($"Server info found. Start traveling to {serverIp}:{serverPort}.");
            OnMatchmakingServerReady?.Invoke(serverIp, serverPort);
            GameManager.Instance.ShowTravelingLoading(() =>
            {
                GameManager.Instance.StartAsClient(serverIp, serverPort, CustomMatchmakingModels.DefaultGameMode);
            },
            CustomMatchmakingModels.TravelingMessage);
        }
        else
        {
            BytewarsLogger.LogWarning($"Cannot travel to server. Unable to parse server info Ip address and port.");
        }
    }

    private bool TryParseServerInfo(
        string serverInfoToParse, 
        out string serverIp, 
        out ushort serverPort)
    {
        serverIp = null;
        serverPort = 0;

        if (string.IsNullOrWhiteSpace(serverInfoToParse))
        {
            BytewarsLogger.LogWarning("Server info is empty.");
            return false;
        }

        // Try to split server Ip and port.
        string[] serverAddressParts = serverInfoToParse.Split(':');
        if (serverAddressParts.Length != 2)
        {
            BytewarsLogger.LogWarning("Server info does not contain Ip address and port.");
            return false;
        }

        // Try to parse server Ip.
        if (!IPAddress.TryParse(serverAddressParts[0], out _) &&
            Uri.CheckHostName(serverAddressParts[0]) == UriHostNameType.Unknown)
        {
            BytewarsLogger.LogWarning("Server info has invalid Ip address.");
            return false;
        }

        // Try to parse server port.
        if (!ushort.TryParse(serverAddressParts[1], out serverPort))
        {
            BytewarsLogger.LogWarning("Server info has invalid port.");
            return false;
        }

        // Parse localhost if any, because it is not supported by Unity's networking.
        serverIp = Utilities.TryParseLocalHostUrl(serverAddressParts[0]);

        return true;
    }
}
