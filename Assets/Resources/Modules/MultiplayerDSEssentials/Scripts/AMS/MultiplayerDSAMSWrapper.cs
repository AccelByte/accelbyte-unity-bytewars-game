// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;

public class MultiplayerDSAMSWrapper : GameSessionUtilityWrapper
{
    public event Action OnAMSConnectionOpened;
    public event Action OnAMSDrainReceived;

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
        ams = AccelByteSDK.GetServerRegistry().GetAMS();
        if (ams == null)
        {
            BytewarsLogger.LogWarning("[AMS] AMS is null");
        }
        else
        {
            ams.OnOpen += OnAMSConnected;
            ams.Disconnecting += OnAMSDisconnecting;
        }
        dsHub = AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
    }

    #region Events Functions

    public void SubscribeAMSEvents()
    {
        if (ams == null)
        {
            return;
        }

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        OnAMSConnectionOpened?.Invoke();
#else
        if(ams.IsConnected)
        {
            OnAMSConnectionOpened?.Invoke();
        }
        else
        {
            ams.OnOpen += OnAMSConnected;
        }
#endif
        ams.OnDrainReceived += () => OnAMSDrainReceived?.Invoke();
    }

    public void SubscribeDSHubEvents()
    {
        dsHub.OnConnected += OnDSHubConnected;
        dsHub.OnDisconnected += OnDSHubDisconnected;
        dsHub.MatchmakingV2ServerClaimed += OnServerClaimed;
    }

    private void OnServerClaimed(Result<ServerClaimedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"error: {result.Error.Message}");
        }
        else
        {
            BytewarsLogger.Log($"ds success claimed {result.Value}");
            GameData.ServerSessionID = result.Value.sessionId;
        }
    }

    private void OnAMSConnected()
    {
        BytewarsLogger.Log("[AMS] AMS Connected!");
        OnAMSConnectionOpened?.Invoke();
        ams.OnOpen -= OnAMSConnected;
    }

    private void OnDSHubConnected()
    {
        BytewarsLogger.Log($"DS connected to DSHub");

    }

    private void OnDSHubDisconnected(WsCloseCode wsCloseCode)
    {
        switch (wsCloseCode)
        {
            case WsCloseCode.Normal:
                BytewarsLogger.Log("DS disconnected from DSHub, trying to reconnect..");
                break;
            case WsCloseCode.Undefined or WsCloseCode.Abnormal or WsCloseCode.NoStatus:
                // Reconnect to DS
                if (!String.IsNullOrEmpty(DedicatedServerId) && dsHub != null)
                {
                    ConnectToDSHub(DedicatedServerId);
                }
                break;
        }
    }

    #endregion

    #region AB Service Functions

    public void LoginWithClientCredentials(ResultCallback resultCallback = null)
    {
        ds?.LoginWithClientCredentials(
            result => OnLoginWithClientCredentialsCompleted(result, resultCallback)
        );
    }

    public void SendReadyMessageToAMS()
    {
        ams?.SendReadyMessage();
    }

    public void ConnectToDSHub(string serverId)
    {
        dsHub?.Connect(serverId);
    }

    public void Disconnect()
    {
        ams.Disconnect();
    }

    private void OnAMSDisconnecting(Result<DisconnectNotif> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("DS disconnected from DSHub, trying to reconnect..");
        }
    }

    #endregion

    #region Callback Functions

    private void OnLoginWithClientCredentialsCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Server login success.");
        }
        else
        {
            BytewarsLogger.Log($"Server login failed. Error code: {result.Error.Code}, message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    #endregion

#endif
}