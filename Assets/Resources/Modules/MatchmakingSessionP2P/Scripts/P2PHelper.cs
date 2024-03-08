// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using Unity.Netcode;
using UnityEngine;

public class P2PHelper
{
    private static AccelByteNetworkTransportManager transportManager;
    private static NetworkManager networkManager;
    private const string ClassName = "[P2PHelper]";

    private static void Init()
    {
        if (transportManager != null)
        {
            return;
        }
        networkManager = NetworkManager.Singleton;
        transportManager = networkManager.gameObject.AddComponent<AccelByteNetworkTransportManager>();
        var apiClient = MultiRegistry.GetApiClient();
        transportManager.Initialize(apiClient);
    }

    public static void StartAsHost(InGameMode gameMode, string matchSessionId)
    {
        SetP2PNetworkTransport(gameMode, matchSessionId);
        networkManager.StartHost();
        Debug.Log($"{ClassName} Start P2P Host");
        GameManager.StartListenNetworkSceneEvent();
    }

    public static void StartAsP2PClient(string hostUserId, InGameMode gameMode, string matchSessionId)
    {
        SetP2PNetworkTransport(gameMode, matchSessionId);
        transportManager.SetTargetHostUserId(hostUserId);
        networkManager.StartClient();
        Debug.Log($"{ClassName} Start P2P Client hostUserId: {hostUserId}");
    }

    private static void SetP2PNetworkTransport(InGameMode gameMode, string matchSessionId)
    {
        Init();
        var data = new InitialConnectionData() { inGameMode = gameMode, serverSessionId = matchSessionId };
        networkManager.NetworkConfig.ConnectionData = GameUtility.ToByteArray(data);
        networkManager.NetworkConfig.NetworkTransport = transportManager;
    }
}
