// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class P2PHelper
{
    private static AccelByteNetworkTransportManager transportManager;
    private static NetworkManager networkManager;

    private static void Init()
    {
        if (transportManager != null)
        {
            return;
        }
        networkManager = NetworkManager.Singleton;
        transportManager = networkManager.gameObject.AddComponent<AccelByteNetworkTransportManager>();
        var apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        transportManager.Initialize(apiClient);
    }

    public static async void StartAsHost(InGameMode gameMode, string matchSessionId)
    {
        await GameManager.ShowTravelingLoading();

        GameManager.Instance.ResetCache();
        GameData.ServerType = ServerType.OnlinePeer2Peer;

        SetP2PNetworkTransport(gameMode, matchSessionId);
        networkManager.StartHost();
        BytewarsLogger.Log($"Start P2P Host");
        GameManager.StartListenNetworkSceneEvent();
    }

    public static async void StartAsP2PClient(string hostUserId, InGameMode gameMode, string matchSessionId)
    {
        await GameManager.ShowTravelingLoading();

        GameManager.Instance.ResetCache();
        GameData.ServerType = ServerType.OnlinePeer2Peer;

        SetP2PNetworkTransport(gameMode, matchSessionId);
        transportManager.SetTargetHostUserId(hostUserId);
        networkManager.StartClient();
        BytewarsLogger.Log($"Start P2P Client hostUserId: {hostUserId}");
    }

    private static void SetP2PNetworkTransport(InGameMode gameMode, string matchSessionId)
    {
        Init();
        var data = new InitialConnectionData() { inGameMode = gameMode, serverSessionId = matchSessionId };
        networkManager.NetworkConfig.ConnectionData = GameUtility.ToByteArray(data);
        networkManager.NetworkConfig.NetworkTransport = transportManager;
    }
}
