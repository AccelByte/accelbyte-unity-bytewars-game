using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reconnect : MonoBehaviour
{
    public void ConnectAsClient(UnityTransport unityTransport, string address, ushort port, 
        InitialConnectionData initialConnectionData)
    {
        if (unityTransport)
        {
            SetNetworkManagerData(unityTransport, address, port, initialConnectionData);
            NetworkManager.Singleton.StartClient();
            //after called StartClient() NetworkManager will call OnClientConnected if the client connected to server successfully
            //TODO set error connect to server callback
        }
        else
        {
            Debug.LogError("can't start as client, unity transport is null");
        }
    }

    private void SetNetworkManagerData(UnityTransport unityTransport, string address, ushort port, 
        InitialConnectionData initialConnectionData)
    {
        unityTransport.ConnectionData.Address = address;
        unityTransport.ConnectionData.Port = port;
        unityTransport.MaxConnectAttempts = GameConstant.MaxConnectAttemptsSec;
        var connectionData = GameUtility.ToByteArray(initialConnectionData);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = connectionData;
    }
    public void StartAsHost(UnityTransport unityTransport, string address, ushort port, 
        InitialConnectionData initialConnectionData)
    {
        if (unityTransport)
        {
            unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
            SetNetworkManagerData(unityTransport, address, port, initialConnectionData);
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            Debug.LogError("can't start as host, unity transport is null");
        }
    }

    private IEnumerator reconnectCoroutine;
    private bool reconnectionInProgress = false;

    public void TryReconnect(InitialConnectionData initialConnectionData)
    {
        if (!reconnectionInProgress)
        {
            reconnectCoroutine = ReconnectWait(initialConnectionData);
            reconnectionInProgress = true;
            StartCoroutine((reconnectCoroutine));
        }
    }

    private readonly WaitForSeconds wait3Seconds = new WaitForSeconds(3);
    private IEnumerator ReconnectWait(InitialConnectionData initialConnectionData)
    {
        yield return DisconnectSafely();
        yield return wait3Seconds;
        var connectionData = GameUtility.ToByteArray(initialConnectionData);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = connectionData;
        Debug.Log("reconnect start client");
        NetworkManager.Singleton.StartClient();
        reconnectionInProgress = false;
    }
    public void OnClientConnected(ulong clientNetworkId, bool isOwner, bool isServer, bool isClient, bool isHost,
        ServerHelper serverHelper, InGameMode inGameMode,
        Dictionary<ulong, GameClientController> connectedClients, InGameState inGameState,
        Dictionary<ulong, Player> players, int gameTimeLeft, ClientHelper clientHelper)
    {
        Debug.Log($"OnClientConnected IsServer:{isServer} isOwner:{isOwner} clientNetworkId:{clientNetworkId}");
        isIntentionallyDisconnect = false;
        var game = GameManager.Instance;
        if (isOwner && isServer)
        {
            serverHelper.StartCoroutineCountdown(this, GameData.GameModeSo.lobbyCountdownSecond, 
                game.OnLobbyCountdownServerUpdated);
            //most variable exists only on IsServer bracket
            bool isInGameScene = GameConstant.GameSceneBuildIndex == SceneManager.GetActiveScene().buildIndex;
            game.SendConnectedPlayerStateClientRpc( serverHelper.ConnectedTeamStates.Values.ToArray(), 
                serverHelper.ConnectedPlayerStates.Values.ToArray(), inGameMode, isInGameScene);
            var playerObj = NetworkManager.Singleton.ConnectedClients[clientNetworkId].PlayerObject;
            var gameClient = playerObj.GetComponent<GameClientController>();
            if (gameClient)
            {
                connectedClients.Add(clientNetworkId, gameClient);
                Debug.Log($"clientNetworkId: {clientNetworkId} connected");
                if (isInGameScene && inGameState!=InGameState.GameOver)
                {
                    if (players.TryGetValue(clientNetworkId, out var serverPlayer))
                    {
                        serverPlayer.UpdateMissilesState();
                        game.ReAddReconnectedPlayerClientRpc(clientNetworkId, serverPlayer.GetFiredMissilesId(),
                            serverHelper.ConnectedTeamStates.Values.ToArray(), 
                            serverHelper.ConnectedPlayerStates.Values.ToArray());
                    }
                    //gameplay already started
                    if (gameTimeLeft != 0)
                    {
                        game.SetInGameState(InGameState.Playing);
                    }
                    else
                    {
                        game.SetInGameState(InGameState.PreGameCountdown);
                    }
                }
            }
        }
        if (isClient && !isHost)
        {
            //most variable does not exists on IsClient bracket
            clientHelper.SetClientNetworkId(clientNetworkId);
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj)
            {
                var gameController = playerObj.GetComponent<GameClientController>();
                if (gameController)
                {
                    connectedClients.TryAdd(clientNetworkId, gameController);
                }
            }
        }
    }
    
    public void OnClientStopped(bool isHost, InGameState inGameState, ServerHelper serverHelper, ulong clientNetworkId,
        InGameMode inGameMode)
    {
        Debug.Log($"OnClientStopped isHost:{isHost} clientNetworkId:{clientNetworkId}");
        if (isHost)
        {
            //QuitToMainMenu();
            return;
        }
        //TODO check is in game scene, check whether client intentionally click quit button
        if (inGameState != InGameState.GameOver)
        {
            if (serverHelper.ConnectedPlayerStates.TryGetValue(clientNetworkId, out var playerState))
            {
                var initialData = new InitialConnectionData()
                {
                    inGameMode = inGameMode,
                    sessionId = playerState.sessionId
                };
                if(!isIntentionallyDisconnect)
                    TryReconnect(initialData);
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
                lobby.ShowStatus("disconnected from server, trying to reconnect...");
            }
        }
    }

    private bool isIntentionallyDisconnect;
    public IEnumerator DisconnectIntentionally()
    {
        isIntentionallyDisconnect = true;
        yield return  DisconnectSafely();
    }

    private IEnumerator DisconnectSafely()
    {
        if (NetworkManager.Singleton.IsListening && 
            NetworkManager.Singleton.IsClient && 
            !NetworkManager.Singleton.ShutdownInProgress)
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
        }
    }
}
