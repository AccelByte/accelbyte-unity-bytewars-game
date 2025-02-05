// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using NativeWebSocket;
#if UNITY_WEBGL
using Netcode.Transports.WebSocket;
using static Netcode.Transports.WebSocket.WebSocketEvent;
#endif

public class CustomMatchmakingWrapper : MonoBehaviour
{
    public Action OnMatchmakingStarted;
    public Action<WebSocketCloseCode /*closeCode*/, string /*closeMessage*/> OnMatchmakingStopped;
    public Action<CustomMatchmakingModels.MatchmakerPayload /*payload*/> OnMatchmakingPayload;
    public Action<string /*serverIp*/, ushort /*serverPort*/> OnMatchmakingServerReady;
    public Action<string /*errorMessage*/> OnMatchmakingError;

    private string pendingCloseMessage = string.Empty;

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

        pendingCloseMessage = string.Empty;

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

    public void CancelMatchmaking(bool isIntentional) 
    {
        // Use generic error message if the cancelation is intentional.
        if (isIntentional)
        {
            pendingCloseMessage = CustomMatchmakingModels.MatchmakingCanceledErrorMessage;
        }

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
#if !UNITY_WEBGL
        nativeWebSocket = null;
#else
        netcodeWebSocket = null;
#endif

        // Store and clear the pending close message.
        string closeMessage = pendingCloseMessage;
        pendingCloseMessage = string.Empty;

        /* Handle WebSocket close conditions:
         * If closed normally with a close message, set the close code to undefined.
         * If closed abnormally, use the generic error message.
         * Otherwise, retain the WebSocket's native close code and message. */
        if (closeCode == WebSocketCloseCode.Normal && !string.IsNullOrEmpty(closeMessage))
        {
            closeCode = WebSocketCloseCode.Undefined;
        }
        else if (closeCode == WebSocketCloseCode.Abnormal)
        {
            closeMessage = CustomMatchmakingModels.MatchmakingErrorMessage;
        }

        BytewarsLogger.Log($"Disconnected from matchmaker. Close code: {closeCode}. Close message: {closeMessage}");
        OnMatchmakingStopped?.Invoke(closeCode, closeMessage);
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
            /* Configure settings to prevent enum deserialization from integer values.
             * The enum value must match the string representation defined in the class.*/
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter { AllowIntegerValues = false } },
                NullValueHandling = NullValueHandling.Ignore
            };

            payload = JsonConvert.DeserializeObject<CustomMatchmakingModels.MatchmakerPayload>(payloadStr, settings);
        }
        catch (Exception e) 
        {
            BytewarsLogger.LogWarning($"Cannot handle matchmaker payload. Unable to parse payload. Error: {e.Message}");
            payload = null;
        }

        // Abort matchmaking if payload is invalid.
        if (payload == null)
        {
            BytewarsLogger.LogWarning("Cannot handle matchmaker payload. Matchmaker payload is null.");
            pendingCloseMessage = CustomMatchmakingModels.MatchmakingInvalidPayloadErrorMessage;
            CancelMatchmaking(isIntentional: false);
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
        pendingCloseMessage = errorMessage;
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
            pendingCloseMessage = CustomMatchmakingModels.MatchmakingInvalidPayloadErrorMessage;
            CancelMatchmaking(isIntentional: false);
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
