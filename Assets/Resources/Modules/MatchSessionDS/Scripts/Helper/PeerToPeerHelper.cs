using System;
using AccelByte.Core;
using Unity.Netcode;
using UnityEngine;

public class PeerToPeerHelper : MonoBehaviour
{
    // private static AccelByteNetworkTransportManager _transportManager;
    private static ApiClient _apiClient;
    private const string ClassName = "[PeerToPeerHelper]";
    private static bool _isInitialized;
    private void Start()
    {
        // Debug.Log($"{ClassName} started");
        // if (_transportManager == null)
        // {
        //     _transportManager = gameObject.AddComponent<AccelByteNetworkTransportManager>();
        // }
    }
    private static void Init()
    {
        if (_isInitialized) return;
        _apiClient = MultiRegistry.GetApiClient();
        // _transportManager.Initialize(_apiClient);
        _isInitialized = true;
    }
    /// <summary>
    /// start as Host for peer to peer (P2P) server type
    /// using AccelByteNetworkTransportManager NetworkTransport 
    /// </summary>
    /// <param name="gameMode">DeathMatch or Elimination game mode</param>
    public static void StartAsP2PHost(InGameMode gameMode)
    {
        SetP2PNetworkTransport(gameMode);
        NetworkManager.Singleton.StartHost();
    }

    public static void StartAsP2PClient(string hostUserId, InGameMode gameMode)
    {
        SetP2PNetworkTransport(gameMode);
        // _transportManager.SetTargetHostUserId(hostUserId);
        NetworkManager.Singleton.StartClient();

    }

    private static void SetP2PNetworkTransport(InGameMode gameMode)
    {
        Init();
        var initialData = new InitialConnectionData() { inGameMode = gameMode };
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            GameUtility.ToByteArray(initialData);
        // NetworkManager.Singleton.NetworkConfig.NetworkTransport = _transportManager;
    }
}
