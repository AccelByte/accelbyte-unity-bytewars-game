// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using Unity.Netcode;

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
        ApiClient apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        transportManager.Initialize(apiClient);

        transportManager.OnTransportEvent += GameManager.Instance.OnTransportEvent;
    }

    public static void StartAsHost(InGameMode gameMode, string matchSessionId)
    {
        GameManager.Instance.ShowTravelingLoading(() => 
        {
            BytewarsLogger.Log($"Start P2P Host");

            GameManager.Instance.ResetCache();
            GameData.ServerType = ServerType.OnlinePeer2Peer;

            SetP2PNetworkTransport(gameMode, matchSessionId);
            networkManager.StartHost();

            GameManager.StartListenNetworkSceneEvent();
        });
    }

    public static void StartAsP2PClient(string hostUserId, InGameMode gameMode, string matchSessionId)
    {
        GameManager.Instance.ShowTravelingLoading(() =>
        {
            BytewarsLogger.Log($"Start P2P Client hostUserId: {hostUserId}");

            GameManager.Instance.ResetCache();
            GameData.ServerType = ServerType.OnlinePeer2Peer;

            SetP2PNetworkTransport(gameMode, matchSessionId);
            transportManager.SetTargetHostUserId(hostUserId);
            networkManager.StartClient();
        });
    }

    private static void SetP2PNetworkTransport(InGameMode gameMode, string matchSessionId)
    {
        Init();
        InitialConnectionData data = new InitialConnectionData() { inGameMode = gameMode, serverSessionId = matchSessionId };
        networkManager.NetworkConfig.ConnectionData = GameUtility.ToByteArray(data);
        networkManager.NetworkConfig.NetworkTransport = transportManager;
    }
}
