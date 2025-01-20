// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
#if UNITY_SERVER
using AccelByte.Core;
using AccelByte.Server;
#endif

public class AMSModuleWrapper : MonoBehaviour
{
#if UNITY_SERVER
    private ServerAMS ams;

    private void OnEnable()
    {
        BytewarsLogger.Log("AMS Module wrapper initialized.");

        // Get AMS interface from SDK with auto create and auto connect to AMS WebSocket enabled.
        ams = AccelByteSDK.GetServerRegistry().GetAMS(autoCreate: true, autoConnect: true);
        if (ams == null) 
        {
            BytewarsLogger.LogWarning("AMS interface is null. You might run the instance in Unity Editor. Try to package the game server as executable.");
            return;
        }
        ams.OnOpen += OnConnected;
        ams.Disconnected += OnDisconnected;
        ams.OnDrainReceived += OnDrainReceived;

        // On-server deregister (e.g. game over), disconnect from AMS.
        GameManager.Instance.OnDeregisterServer += Disconnect;
    }

    private void OnDisable()
    {
        BytewarsLogger.Log("AMS Module wrapper deinitialized.");

        if (ams == null)
        {
            BytewarsLogger.LogWarning("AMS interface is null. You might run the instance in Unity Editor. Try to package the game server as executable.");
            return;
        }

        // Unbind events.
        ams.OnOpen -= OnConnected;
        ams.Disconnected -= OnDisconnected;
        ams.OnDrainReceived -= OnDrainReceived;
        GameManager.Instance.OnDeregisterServer -= Disconnect;
    }

    private void SendServerReady()
    {
        if (!ams.IsConnected)
        {
            BytewarsLogger.LogWarning("Cannot set server ready. The AMS websocket connection is not established.");
            return;
        }

        BytewarsLogger.Log("Send server ready message to AMS.");
        ams.SendReadyMessage();
    }

    private void Disconnect() 
    {
        if (!ams.IsConnected) 
        {
            BytewarsLogger.LogWarning("Cannot disconnect AMS websocket. The AMS websocket connection is not established.");
            return;
        }

        BytewarsLogger.Log("Disconnecting from AMS websocket.");
        ams.Disconnect();
    }

    private void OnConnected() 
    {
        BytewarsLogger.Log("Success to connect to AMS websocket.");
        
        /* It is not required to set the server as ready immediately after the AMS websocket connection is established.
         * If the server needs to perform setup tasks before welcoming the player, the server ready message should be sent afterward.
         * Since Byte Wars does not require such setup, the server ready message is sent immediately here. */
        SendServerReady();
    }

    private void OnDisconnected(WsCloseCode wsCloseCode) 
    {
        BytewarsLogger.Log($"Disconnected from AMS websocket. Ws Code: {wsCloseCode}");
        
        if (wsCloseCode == WsCloseCode.Normal) 
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }

    private void OnDrainReceived() 
    {
        BytewarsLogger.Log("Drain event received.");

        /* When a drain event occurs, the server may perform some clean-up tasks.
         * Drain behavior on Byte Wars:
         * If the server is not serving any game session, then disconnect the server from AMS and shut it down.
         * Otherwise, keep it running as normal. */
        if (GameManager.Instance.InGameMode == InGameMode.None && GameManager.Instance.ConnectedPlayerStates.Count <= 0) 
        {
            BytewarsLogger.Log("There is no game is in progress. Handle drain event to shut down the server.");
            Disconnect();
            return;
        }

        BytewarsLogger.Log("The game is in progress. Ignore drain event.");
    }
#endif
}
