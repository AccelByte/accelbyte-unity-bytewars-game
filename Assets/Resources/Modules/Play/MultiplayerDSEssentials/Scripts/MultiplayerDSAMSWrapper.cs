// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class MultiplayerDSAMSWrapper : MonoBehaviour
{
    public event Action OnAMSConnectionOpened = delegate { };
    public event Action OnAMSConnectionClosed = delegate { };
    public event Action OnAMSDrainSignalReceived = delegate { };

    private DedicatedServer ds;
    private ServerAMS ams;
    private ServerDSHub dsHub;

    private readonly int ServerShutdownDelay = 10;

#if UNITY_SERVER
    private void OnEnable()
    {
        ams = AccelByteSDK.GetServerRegistry().GetAMS(autoCreate: true, autoConnect: true);
        if (ams == null) 
        {
            BytewarsLogger.LogWarning("AMS interface is invalid. This interface only support packaged server using development build.");
            return;
        }

        ds = AccelByteSDK.GetServerRegistry().GetApi().GetDedicatedServer();
        if (ds == null) 
        {
            BytewarsLogger.LogWarning("Dedicated Server interface is invalid. This interface only support packaged server using development build.");
            return;
        }

        dsHub = AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
        if (dsHub == null)
        {
            BytewarsLogger.LogWarning("DSHub interface is invalid. This interface only support packaged server using development build.");
            return;
        }

        // Bind AMS events.
        ams.OnOpen += OnAMSConnected;
        ams.Disconnected += OnAMSDisconnected;
        ams.OnDrainReceived += OnAMSDrainReceived;

        // Bind DSHub events.
        dsHub.OnConnected += OnDSHubConnected;
        dsHub.OnDisconnected += OnDSHubDisconnected;
        dsHub.MatchmakingV2ServerClaimed += OnServerClaimed;
        dsHub.GameSessionV2Ended += OnGameSessionEnded;

        // Bind server actions.
        OnAMSConnectionClosed += ShutdownServer;
        OnAMSDrainSignalReceived += DeregisterServer;
        GameManager.Instance.OnDeregisterServer += DeregisterServer;

        // Login and register server to AMS.
        LoginServer((result) =>
        {
            if (ams.IsConnected)
            {
                RegisterServer();
            }
            else
            {
                ams.OnOpen += RegisterServer;
            }
        });
    }

    private void OnDisable()
    {
        // Unbind AMS events.
        if (ams != null)
        {
            ams.OnOpen -= OnAMSConnected;
            ams.Disconnected -= OnAMSDisconnected;
            ams.OnDrainReceived -= OnAMSDrainReceived;
        }

        // Unbind DSHub events.
        if (dsHub != null) 
        {
            dsHub.OnConnected -= OnDSHubConnected;
            dsHub.OnDisconnected -= OnDSHubDisconnected;
            dsHub.MatchmakingV2ServerClaimed -= OnServerClaimed;
            dsHub.GameSessionV2Ended -= OnGameSessionEnded;
        }

        // Unbind server actions.
        OnAMSConnectionClosed -= ShutdownServer;
        OnAMSDrainSignalReceived -= DeregisterServer;
        GameManager.Instance.OnDeregisterServer -= DeregisterServer;
    }

    private void LoginServer(ResultCallback onComplete)
    {
        BytewarsLogger.Log("Start server login.");

        ds?.LoginWithClientCredentials((result) => 
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to login server. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.LogWarning("Success to login server.");
            }

            onComplete?.Invoke(result);
        });
    }

    private void RegisterServer()
    {
        ams.OnOpen -= RegisterServer;

        string dsId = AccelByteSDK.GetServerConfig().DsId;
        if (string.IsNullOrEmpty(dsId))
        {
            BytewarsLogger.LogWarning("Failed to register server. DSId is invalid.");
            return;
        }

        BytewarsLogger.Log("Registering server.");

        ams?.SendReadyMessage();
        dsHub?.Connect(dsId);
    }

    private void DeregisterServer()
    {
        BytewarsLogger.Log("Deregistering server.");
        ams?.Disconnect();
        dsHub?.Disconnect();
    }

    private void ShutdownServer()
    {
        BytewarsLogger.Log("Shutting down server.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    private void OnAMSConnected()
    {
        BytewarsLogger.Log("Server connected to AMS.");
        OnAMSConnectionOpened?.Invoke();
    }

    private void OnAMSDisconnected(WsCloseCode closeCode)
    {
        BytewarsLogger.Log($"Server disconnected from AMS. Close code: {closeCode}");
        OnAMSConnectionClosed?.Invoke();
    }

    private void OnAMSDrainReceived()
    {
        // Get the delay config from launch param.
        const string keyword = "-DrainLogicDelayInSecs=";
        if (!float.TryParse(TutorialModuleUtil.GetLaunchParamValue(keyword), out float delay))
        {
            delay = ServerShutdownDelay;
            BytewarsLogger.Log("Could not parse drain delay launch param. Fallback to use default delay.");
        }

        // Execute drain logic after a delay to accommodate session info update delay.
        BytewarsLogger.Log($"DS received drain signal from AMS. Delaying {delay} seconds to execute drain logic.");
        Invoke(nameof(ExecuteDrainSignal), delay);
    }

    private void ExecuteDrainSignal()
    {
        if (string.IsNullOrEmpty(GameData.ServerSessionID))
        {
            BytewarsLogger.Log("ServerSessionID is empty, executing drain logic now!");
            OnAMSDrainSignalReceived?.Invoke();
        }
        else
        {
            BytewarsLogger.Log("ServerSessionID is not empty, drain ignored until session ends.");
        }
    }

    private void OnDSHubConnected()
    {
        BytewarsLogger.Log($"Server connected to DSHub");
    }

    private void OnDSHubDisconnected(WsCloseCode closeCode)
    {
        BytewarsLogger.Log($"Server disconnected from DSHub. Close code: {closeCode}");

        // If disconnected abnormally, try to reconnect.
        if (closeCode is WsCloseCode.Undefined or WsCloseCode.Abnormal or WsCloseCode.NoStatus)
        {
            string dsId = AccelByteSDK.GetServerConfig().DsId;
            if (string.IsNullOrEmpty(dsId))
            {
                BytewarsLogger.LogWarning("Failed to reconnecting server to DSHub. DSId is invalid.");
            }

            BytewarsLogger.Log("Reconnecting server to DSHub.");
            dsHub?.Connect(dsId);
        }
    }

    private void OnServerClaimed(Result<ServerClaimedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to claim server. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Success to claim server. Session ID: {result.Value.sessionId}");
        GameData.ServerSessionID = result.Value.sessionId;
    }

    private void OnGameSessionEnded(Result<SessionEndedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed handle on game session ended. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Recieved game session ended. Session ID: {result.Value.SessionId}. Shutting down server.");
        GameManager.Instance.StartShutdownCountdown(ServerShutdownDelay);
    }
#endif
}