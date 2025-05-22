// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Netcode.Transports.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reconnect : MonoBehaviour
{
    private IEnumerator reconnectCoroutine;
    private bool reconnectionInProgress = false;

    private const int reconnectWaitTimeInSeconds = 3;

    private bool isIntentionallyDisconnect;

    public void ConnectAsClient(
        WebSocketTransport networkTransport, 
        string address, 
        ushort port,
        InitialConnectionData initialConnectionData)
    {
        if (networkTransport)
        {
            BytewarsLogger.Log($"Starting client to connect to {address}:{port}");
            SetNetworkManagerData(networkTransport, address, port, initialConnectionData);
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            BytewarsLogger.LogWarning("Cannot start as client, unity transport is null");
        }
    }

    private void SetNetworkManagerData(
        WebSocketTransport networkTransport, 
        string address, 
        ushort port,
        InitialConnectionData initialConnectionData)
    {
        if (!networkTransport) 
        {
            BytewarsLogger.LogWarning("Failed to set network manager data. Network transport is null.");
            return;
        }

        // Enable secure websocket connection (WSS) and proxy only for packaged WebGL build.
        bool isRequireSecureConnection = false;
#if (UNITY_WEBGL && !UNITY_EDITOR)
        isRequireSecureConnection = true;
#endif

        ProxyConfiguration proxy = TutorialModuleUtil.GetProxy();
        string proxyUrl = proxy.url;
        string proxyPath = proxy.path.Replace("{server_ip}", address).Replace("{server_port}", port.ToString());

        // If require secure connection but the proxy is empty, abort to use secure connection.
        if (isRequireSecureConnection && (string.IsNullOrEmpty(proxyUrl) || string.IsNullOrEmpty(proxyPath))) 
        {
            BytewarsLogger.LogWarning("Failed to enable secure WebSocket connection (WSS). The proxy setting is empty.");
            isRequireSecureConnection = false;
        }

        networkTransport.ConnectAddress = isRequireSecureConnection ? proxyUrl : address;
        networkTransport.Path = isRequireSecureConnection ? proxyPath : "/";
        networkTransport.Port = isRequireSecureConnection ? (ushort)443 : port;
        networkTransport.SecureConnection = isRequireSecureConnection;
        networkTransport.AllowForwardedRequest = true;
        networkTransport.CertificateBase64String = string.Empty;
        networkTransport.AuthUsername = proxy.username;
        networkTransport.AuthPassword = proxy.password;

        byte[] connectionData = GameUtility.ToByteArray(initialConnectionData);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = connectionData;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = networkTransport;
    }

    public void StartAsHost(
        WebSocketTransport networkTransport, 
        string address, 
        ushort port,
        InitialConnectionData initialConnectionData)
    {
        if (networkTransport)
        {
            SetNetworkManagerData(networkTransport, address, port, initialConnectionData);
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            BytewarsLogger.LogWarning("Cannot start as host, unity transport is null");
        }
    }

    public void TryReconnect(InitialConnectionData initialConnectionData)
    {
        if (!reconnectionInProgress)
        {
            reconnectCoroutine = ReconnectWait(initialConnectionData);
            reconnectionInProgress = true;
            StartCoroutine((reconnectCoroutine));
        }
    }

    private IEnumerator ReconnectWait(InitialConnectionData initialConnectionData)
    {
        yield return DisconnectSafely();
        yield return new WaitForSeconds(reconnectWaitTimeInSeconds);

        byte[] connectionData = GameUtility.ToByteArray(initialConnectionData);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = connectionData;
        BytewarsLogger.Log("Reconnect start client");
        NetworkManager.Singleton.StartClient();
        
        reconnectionInProgress = false;
    }

    public void OnClientConnected(
        ulong clientNetworkId, 
        bool isOwner, 
        bool isServer, 
        bool isClient, 
        bool isHost,
        ServerHelper serverHelper, 
        InGameMode inGameMode,
        Dictionary<ulong, GameClientController> connectedClients, 
        InGameState inGameState, 
        ServerType serverType,
        Dictionary<ulong, Player> players, 
        int gameTimeLeft, 
        ClientHelper clientHelper)
    {
        BytewarsLogger.Log($"OnClientConnected IsServer:{isServer} isOwner:{isOwner} clientNetworkId:{clientNetworkId}");

        isIntentionallyDisconnect = false;

        // If server, handle player authentication.
        if (isOwner && isServer)
        {
            NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientNetworkId].PlayerObject;
            GameClientController gameClient = playerObj.GetComponent<GameClientController>();
            bool isInGameScene = GameConstant.GameSceneBuildIndex == SceneManager.GetActiveScene().buildIndex;

            if (gameClient)
            {
                BytewarsLogger.Log($"ClientNetworkId: {clientNetworkId} connected.");

                connectedClients.TryAdd(clientNetworkId, gameClient);

                // If in in-game scene.
                if (isInGameScene)
                {
                    if (inGameState != InGameState.GameOver)
                    {
                        // If the player was exists, recover it states.
                        if (players.TryGetValue(clientNetworkId, out Player player))
                        {
                            player.UpdateMissilesState();
                            GameManager.Instance.ReAddReconnectedPlayerClientRpc(
                                clientNetworkId,
                                player.GetFiredMissilesId(),
                                serverHelper.ConnectedTeamStates.Values.ToArray(),
                                serverHelper.ConnectedPlayerStates.Values.ToArray(),
                                GameManager.Instance.CreatedLevel);
                        }

                        // Set game state accordingly.
                        if (gameTimeLeft > 0)
                        {
                            GameManager.Instance.SetInGameState(InGameState.Playing, true);
                        }
                        else
                        {
                            GameManager.Instance.SetInGameState(InGameState.PreGameCountdown, true);
                        }
                    }
                }
                // If in main menu.
                else
                {
                    // Start match lobby countdown.
                    if (GameData.GameModeSo.LobbyCountdownSecond > -1)
                    {
                        serverHelper.StartCoroutineCountdown(this,
                            GameData.GameModeSo.LobbyCountdownSecond,
                            GameManager.Instance.OnLobbyCountdownServerUpdated);
                    }
                }

                GameManager.Instance.SendConnectedPlayerStateClientRpc(
                    serverHelper.ConnectedTeamStates.Values.ToArray(),
                    serverHelper.ConnectedPlayerStates.Values.ToArray(),
                    inGameMode, serverType, isInGameScene);
            }
            else
            {
                // Kick player if player's network object is not exists.
                NetworkManager.Singleton.DisconnectClient(clientNetworkId, "Player network object is not exists.");
            }
        }

        // If client, refresh connected clients.
        if (isClient && !isHost)
        {
            clientHelper.SetClientNetworkId(clientNetworkId);
            NetworkObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj)
            {
                GameClientController gameController = playerObj.GetComponent<GameClientController>();
                if (gameController)
                {
                    connectedClients.TryAdd(clientNetworkId, gameController);
                }
            }
        }
    }

    public void OnClientStopped(
        bool isHost, 
        InGameState inGameState, 
        ServerHelper serverHelper, 
        ulong clientNetworkId,
        InGameMode inGameMode)
    {
        BytewarsLogger.Log($"OnClientStopped isHost:{isHost} clientNetworkId:{clientNetworkId}");
        if (isHost)
        {
            GameManager.Instance.ResetCache();
            StartCoroutine(GameManager.Instance.QuitToMainMenu());
            return;
        } 
        else 
        {
            //TODO check is in game scene, check whether client intentionally click quit button
            if (inGameState != InGameState.GameOver)
            {
                if (serverHelper.ConnectedPlayerStates.TryGetValue(clientNetworkId, out var playerState))
                {
                    InitialConnectionData initialData = new InitialConnectionData()
                    {
                        inGameMode = inGameMode,
                        sessionId = playerState.SessionId,
                        userId = playerState.PlayerId
                    };

                    if (!isIntentionallyDisconnect)
                    {
                        TryReconnect(initialData);
                    }
                    bool isInGameScene = GameConstant.GameSceneBuildIndex == SceneManager.GetActiveScene().buildIndex;
                    GameManager.Instance.RemoveConnectedClient(clientNetworkId, isInGameScene);
                }
            }

            bool isInMainMenu = GameConstant.MenuSceneBuildIndex == SceneManager.GetActiveScene().buildIndex;
            if (isInMainMenu)
            {
                var menuCanvas = MenuManager.Instance.GetCurrentMenu();
                if (menuCanvas && menuCanvas is MatchLobbyMenu lobby)
                {
                    if(!isIntentionallyDisconnect) 
                    {
                        lobby.ShowStatus("Disconnected from server, trying to reconnect...");
                    }
                }
            }            
        }
    }

    public IEnumerator ClientDisconnectIntentionally()
    {
        isIntentionallyDisconnect = true;
        yield return DisconnectSafely();
    }

    private IEnumerator DisconnectSafely()
    {
        if (NetworkManager.Singleton.IsListening &&
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.ShutdownInProgress)
        {
            BytewarsLogger.LogWarning($"Disconnect gracefully.");

            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
        }
    }

    public bool IsServerShutdownOnLobby(int connectedClientCount)
    {
        if (connectedClientCount < 1)
        {
            if (GameData.GameModeSo != null &&
                GameData.GameModeSo.LobbyShutdownCountdown > -1)
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerator ShutdownServer(Action onServerShutdownFinished)
    {
        if (NetworkManager.Singleton.IsListening &&
            NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.ShutdownInProgress)
        {
            BytewarsLogger.Log($"Shutting down server");
            
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            onServerShutdownFinished?.Invoke();
        }
    }
}
