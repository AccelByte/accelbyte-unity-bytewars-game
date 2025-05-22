// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;
using WebSocketSharp;

public class MultiplayerDSAMSWrapper : GameSessionUtilityWrapper
{
    public event Action OnAMSConnectionOpened = delegate {};
    public event Action OnAMSConnectionClosed = delegate {};
    public event Action OnAMSDrainSignalReceived = delegate {};

    private DedicatedServer ds;
    private ServerAMS ams;
    private ServerDSHub dsHub;
    private ServerOauthLoginSession serverOauthLoginSession;
    public string DedicatedServerId
    {
        get
        {
            return AccelByteSDK.GetServerConfig().DsId;
        }
    }

#if UNITY_SERVER
    void Start()
    {
        ds = AccelByteSDK.GetServerRegistry().GetApi().GetDedicatedServer();
        if (ds == null)
        {
            BytewarsLogger.LogWarning("AccelByte Dedicated Server interface is null");
            return;
        }

        ams = AccelByteSDK.GetServerRegistry().GetAMS();
        if (ams == null)
        {
            BytewarsLogger.LogWarning("AccelByte AMS interface is null");
            return;
        }

        dsHub = AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
        if (dsHub == null)
        {
            BytewarsLogger.LogWarning("AccelByte DSHub interface is null");
            return;
        }
    }

    public void LoginWithClientCredentials(ResultCallback resultCallback = null)
    {
        BytewarsLogger.Log("Logging in DS to AccelByte.");

        ds?.LoginWithClientCredentials(
            result => OnLoginWithClientCredentialsCompleted(result, resultCallback)
        );
    }

    private void OnLoginWithClientCredentialsCompleted(Result result, ResultCallback callback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("DS login success.");
        }
        else
        {
            BytewarsLogger.Log($"DS login failed. Error code: {result.Error.Code}, message: {result.Error.Message}");
        }

        callback?.Invoke(result);
    }

    #region AMS Functions

    public void SendReadyMessageToAMS()
    {
        BytewarsLogger.Log("Sending DS ready to AMS.");
        ams?.SendReadyMessage();
    }

    public void DisconnectFromAMS()
    {
        BytewarsLogger.Log($"Disconnecting DS from AMS.");
        ams?.Disconnect();
    }

    public void SubscribeAMSEvents()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        OnAMSConnected();
#else
        if(ams.IsConnected)
        {
            OnAMSConnected();
        }
        else
        {
            ams.OnOpen += OnAMSConnected;
        }
#endif
        ams.OnDrainReceived += OnAMSDrainReceived;
        ams.Disconnected += OnAMSDisconnected;
    }

    private void OnAMSConnected()
    {
        BytewarsLogger.Log("DS is connected to AMS.");
        OnAMSConnectionOpened?.Invoke();
        ams.OnOpen -= OnAMSConnected;
    }

    private void OnAMSDisconnected(WsCloseCode wsCloseCode)
    {
        BytewarsLogger.Log($"DS disconnected from AMS. Disconnect code: {wsCloseCode}");

        // If disconnected intentionally, continue to disconnect DS from DSHub.
        if (wsCloseCode.Equals(WsCloseCode.Normal))
        {
            BytewarsLogger.Log("Disconnecting DS from DSHub.");
            ams.Disconnected -= OnAMSDisconnected;
            dsHub.Disconnect();
        }
    }

    private void OnAMSDrainReceived()
    {
        // Retrieve delay config from launch param.
        const string keyword = "-DrainLogicDelayInSecs=";
        string delayInString = TutorialModuleUtil.GetLaunchParamValue(keyword);
        if (!float.TryParse(delayInString, out float delay))
        {
            BytewarsLogger.Log("Given launch param value can't be parse to float. Using the default 5 seconds delay.");
            delay = 5.0f;
        }
        
        // Execute drain logic after a delay to accomodate session info update delay.
        BytewarsLogger.Log($"DS received drain signal from AMS. Delaying {delay} seconds to execute drain logic.");
        Invoke(nameof(ExecuteDrainSignal), delay);
    }

    private void ExecuteDrainSignal()
    {
        // Only execute if current session isn't active
        if (GameData.ServerSessionID.IsNullOrEmpty())
        {
            BytewarsLogger.Log("ServerSessionID is empty, executing drain logic now!");
            OnAMSDrainSignalReceived?.Invoke();
        }
        else
        {
            BytewarsLogger.Log("ServerSessionID is not empty, drain ignored.");
        }
    }

    #endregion

    #region DSHub Functions

    public void ConnectToDSHub(string serverId)
    {
        BytewarsLogger.Log($"Connecting DS to DSHub by dsid {serverId}");
        dsHub?.Connect(serverId);
    }

    public void SubscribeDSHubEvents()
    {
        dsHub.OnConnected += OnDSHubConnected;
        dsHub.OnDisconnected += OnDSHubDisconnected;
        dsHub.MatchmakingV2ServerClaimed += OnServerClaimed;
        dsHub.GameSessionV2Ended += OnGameSessionEnded;
        dsHub.GameSessionV2MemberChanged += OnGameSessionMemberChanged;
    }

    private void OnDSHubConnected()
    {
        BytewarsLogger.Log($"DS connected to DSHub");
    }

    private void OnDSHubDisconnected(WsCloseCode wsCloseCode)
    {
        BytewarsLogger.Log($"DS disconnected from DSHub. Disconnect code: {wsCloseCode}");

        switch (wsCloseCode)
        {
            case WsCloseCode.Normal:
                // DS disconnected from DSHub intentionally. Signal that the DS connection is closed.
                OnAMSConnectionClosed?.Invoke();
                break;
            case WsCloseCode.Undefined or WsCloseCode.Abnormal or WsCloseCode.NoStatus:
                if (!string.IsNullOrEmpty(DedicatedServerId) && dsHub != null)
                {
                    BytewarsLogger.Log("DS disconnected from DSHub unexpectedly. Trying to reconnect.");
                    ConnectToDSHub(DedicatedServerId);
                }
                else
                {
                    BytewarsLogger.LogWarning("DS disconnected from DSHub unexpectedly and unable to reconnect because of the dsid is empty.");
                }
                break;
        }
    }

    #endregion

    private void OnServerClaimed(Result<ServerClaimedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to claim DS. Error: {result.Error.Message}");
        }
        else
        {
            BytewarsLogger.Log($"Success to claim DS: {result.Value.sessionId}");
            GameData.ServerSessionID = result.Value.sessionId;
        }
    }

    public void OnGameSessionMemberChanged(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log(result.Value.ToJsonString());
        }
        else
        {
            BytewarsLogger.LogWarning($"Failed to receive session member changed notification, code: {result.Error.Code} reason: {result.Error.Message}");
        }
    }

    private void OnGameSessionEnded(Result<SessionEndedNotification> result)
    {
        if (!result.IsError)
        {
            GameManager.Instance.StartShutdownCountdown(10);
            BytewarsLogger.Log(result.Value.ToJsonString());
        }
        else
        {
            BytewarsLogger.LogWarning($"Failed to receive session ended notification, code: {result.Error.Code} reason: {result.Error.Message}");
        }
    }

#endif
}