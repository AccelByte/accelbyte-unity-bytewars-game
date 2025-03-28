﻿// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Netcode.Transports.WebSocket;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public enum GameOverReason
    {
        MatchEnded,
        Abandoned
    }

    public static GameManager Instance { get; private set; }
    
    public delegate void OnGameEndedDelegate(GameOverReason reason);
    public delegate void OnGameStateChangedDelegate(InGameState newGameState);
    public delegate void OnPlayerEnterGameDelegate(PlayerState playerState);
    public delegate void OnDisconnectedInMainMenuDelegate(string disconnectReason);

    public delegate void OnMissileDestroyedDelegate(
        PlayerState owningPlayerState,
        MissileState missileState,
        GameEntityDestroyReason destroyReason,
        PlanetState hitPlanetState,
        PlayerState hitPlayerState);

    public delegate void OnPlayerDieDelegate(
        PlayerState deathPlayerState,
        GameEntityDestroyReason destroyReason,
        PlayerState killerPlayerState,
        MissileState missileState);

    public static event Action OnGameStarted = delegate { };
    public static event OnGameEndedDelegate OnGameEnded = delegate { };
    public static event OnGameStateChangedDelegate OnGameStateChanged = delegate { };

    public static event OnPlayerEnterGameDelegate OnPlayerEnterGame = delegate { };
    public static event OnPlayerDieDelegate OnPlayerDie = delegate { };
    public static event OnMissileDestroyedDelegate OnMissileDestroyed = delegate { };

    public static event OnDisconnectedInMainMenuDelegate OnDisconnectedInMainMenu = delegate { };

    public event Action OnGameStateIsNone = delegate { };
    public event Action OnRegisterServer = delegate { };
    public event Action OnDeregisterServer = delegate { };
    public event Action OnRejectBackfill = delegate { };
    public event Action OnClientLeaveSession = delegate { };

    public Guid CurrentGuid { get; private set; }
    public InGameState InGameState { get; private set; } = InGameState.None;
    public GameModeEnum GameMode { get; private set; } = GameModeEnum.MainMenu;
    public InGameMode InGameMode { get; private set; } = InGameMode.None;
    public InGamePause InGamePause { get; private set; }
    public List<GameEntityAbs> ActiveGEs { get; } = new();
    public Dictionary<ulong, Player> Players { get; } = new();
    public ObjectPooling Pool { get; private set; }
    public LevelCreationResult CreatedLevel { get; private set; }
    public Camera MainCamera => mainCamera;
    public TeamState[] TeamStates => serverHelper.GetTeamStates();
    public Dictionary<int, TeamState> ConnectedTeamStates => serverHelper.ConnectedTeamStates;
    public Dictionary<ulong, PlayerState> ConnectedPlayerStates => serverHelper.ConnectedPlayerStates;
    public ulong ClientNetworkId => clientHelper.ClientNetworkId;
    public bool IsDedicatedServer { get { return IsServer && !IsHost && !IsClient; } }
    public float CurrentRoundTripTime { get; private set; } = 0f;
    public bool IsLocalGame => !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;

    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InGameHUD hud;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameEntityAbs[] gamePrefabs;
    [SerializeField] private FxEntity[] fxPrefabs;
    [SerializeField] private GameModeSO[] availableInGameMode;
    [SerializeField] private Reconnect reconnect;
    [SerializeField] private Transform container;
    [SerializeField] private InGameCamera inGameCamera;

    private const string NotEnoughPlayer = "Not enough players, shutting down DS in: ";
    
    private readonly Dictionary<ulong, GameClientController> connectedClients = new();
    private readonly Dictionary<string, GameEntityAbs> gamePrefabDict = new();
    private readonly Dictionary<int, Planet> planets = new();
    private readonly ConnectionHelper connectionHelper = new();
    private readonly ServerHelper serverHelper = new();
    private readonly ClientHelper clientHelper = new();
    private HashSet<ulong> clientsInGame = new HashSet<ulong>();

    private List<Vector3> availablePositions;
    private WebSocketTransport networkTransport;
    private DebugImplementation debug;
    private MenuManager menuManager;
    private int gameTimeLeft;

    private readonly int TravelingDelay = 1;
    private readonly int TravelingTimeOut = 60;
    private readonly string TravelingMessage = "Traveling";
    private readonly string StartingGameMessage = "Starting Game";

    private readonly string PingNamedMessage = "PingMessage";
    private readonly string PongNamedMessage = "PongMessage";
    private float pingServerRequestTime = 0f;

    private bool isGameStarted = false;
    private bool isGameEnded = false;

    #region Initialization and Lifecycle

    [RuntimeInitializeOnLoadMethod]
    private static void CreateInstance()
    {
        bool isMainMenuScene = SceneManager.GetActiveScene().buildIndex == GameConstant.MenuSceneBuildIndex;
        if (isMainMenuScene && Instance == null)
        {
            Instance = Instantiate(AssetManager.Singleton.GameManagerPrefab);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        foreach (GameEntityAbs gamePrefab in gamePrefabs)
        {
            gamePrefabDict.Add(gamePrefab.name, gamePrefab);
        }

        NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        NetworkManager.Singleton.LogLevel = LogLevel.Developer;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        if (networkTransport == null)
        {
            networkTransport = (WebSocketTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            networkTransport.OnTransportEvent += OnTransportEvent;
        }

#if UNITY_SERVER
        StartServer();
#endif

        debug ??= new DebugImplementation();
        hud.Reset();
        
        InitMenuManagerWhenReady().ContinueWith(() =>
        {
            if (IsServer)
            {
                menuManager.CloseMenuPanel();
                OnRegisterServer?.Invoke();
            }

            InGamePause = new InGamePause(menuManager, hud, this);
        });
    }

    public void OnDisable()
    {
        Pool?.DestroyAll();
        Pool?.ClearAll();
        Players?.Clear();
        BytewarsLogger.Log("GameManager OnDisable");
        serverHelper.CancelCountdown();
    }
    
    private async UniTask InitMenuManagerWhenReady()
    {
        while (!MenuManager.Instance.IsInitiated)
        {
            await UniTask.Delay(50);
        }
        
        menuManager = MenuManager.Instance;
        menuManager.SetEventSystem(eventSystem);
    }

    private void SetupGame()
    {
        // Only offline game, server, and host are able to setup the game.
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) 
        {
            return;
        }

        if (!InGameState.Equals(InGameState.None))
        {
            BytewarsLogger.LogWarning("Cannot setup game. Game is already setup.");
            return;
        }

        if (!SetInGameState(InGameState.Initializing))
        {
            BytewarsLogger.LogWarning("Cannot setup game. Failed to initialize game state.");
            return;
        }

        BytewarsLogger.Log("Setup Game");
        CurrentGuid = Guid.NewGuid();

        Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);

        ActiveGEs.RemoveAll(ge => !ge);
        Players.Clear();
        
        if (GameMode == GameModeEnum.OnlineMultiplayer)
        {
            SetupOnlineGame();
        }
        else
        {
            SetupOfflineGame();
        }
    }
    
    private void SetupOfflineGame()
    {
        InGameStateResult inGameState = InGameFactory.CreateLocalGameState(GameData.GameModeSo);
        serverHelper.SetTeamAndPlayerState(inGameState);
        
        CreateLevelAndInitializeHud(inGameState.TeamStates, inGameState.PlayerStates, out _);
        
        // Disable player inputs except the main player in the single player mode.
        bool isSinglePlayer = GameMode == GameModeEnum.SinglePlayer;
        if (isSinglePlayer && Players.TryGetValue(1, out Player player))
        {
            player.PlayerInput.enabled = false;
        }
        
        SetInGameState(InGameState.PreGameCountdown);
    }
    
    private void SetupOnlineGame()
    {
        if (!IsServer)
        {
            return;
        }
        
        CreateLevelAndInitializeHud(
            serverHelper.ConnectedTeamStates,
            serverHelper.ConnectedPlayerStates,
            out LevelCreationResult levelResult);
        CreatedLevel = levelResult;

        PlaceObjectsClientRpc(
            CreatedLevel,
            serverHelper.ConnectedTeamStates.Values.ToArray(),
            serverHelper.ConnectedPlayerStates.Values.ToArray());
    }
    
    private void CreateLevelAndInitializeHud(
        Dictionary<int, TeamState> teamStates,
        Dictionary<ulong, PlayerState> playerStates,
        out LevelCreationResult result)
    {
        result = InGameFactory.CreateLevel(GameData.GameModeSo, ActiveGEs, Players, Pool, teamStates, playerStates);
        availablePositions = result.AvailablePositions.ToList();
        
        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Init(teamStates.Values.ToArray(), playerStates.Values.ToArray());
        }
    }
    
    public void ResetCache()
    {
        CreatedLevel = null;
        isGameStarted = false;
        isGameEnded = false;
        GameMode = GameModeEnum.MainMenu;
        InGameMode = InGameMode.None;
        connectedClients.Clear();
        clientsInGame.Clear();
        serverHelper.Reset();
    }

    #endregion

    #region Connection Management

    private void OnServerStarted()
    {
        BytewarsLogger.Log("Server started.");

        if (NetworkManager.Singleton.CustomMessagingManager != null) 
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(PingNamedMessage, OnReceivePing);
        }
    }

    private void OnServerStopped(bool isHostStopped)
    {
        BytewarsLogger.Log($"Server stopped. Is host: {isHostStopped}");

        if (NetworkManager.Singleton.CustomMessagingManager != null) 
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(PingNamedMessage);
        }

        if (isHostStopped)
        {
            InGameMode = InGameMode.None;
        }

        ResetCache();
    }

    private void OnClientStarted()
    {
        BytewarsLogger.Log("Client started.");

        if (NetworkManager.Singleton.CustomMessagingManager != null) 
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(PongNamedMessage, OnReceivePong);
        }
    }

    private void OnClientStopped(bool isHostStopped)
    {
        BytewarsLogger.Log($"Client stopped. Is host: {isHostStopped}"); 

        if (NetworkManager.Singleton.CustomMessagingManager != null) 
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(PongNamedMessage);
        }

        if (isHostStopped) 
        {
            ResetCache();
        }

        reconnect.OnClientStopped(isHostStopped, InGameState, serverHelper,
            clientHelper.ClientNetworkId, InGameMode);
    }

    private void StartServer()
    {
        if (!networkTransport)
        {
            BytewarsLogger.Log("Failed to start server. Network transport is null.");
            return;
        }

        GameData.ServerType = ServerType.OnlineDedicatedServer;

        networkTransport.ConnectAddress = ConnectionHandler.GetLocalIPAddress();
        networkTransport.Path = "/";
        networkTransport.Port = ConnectionHandler.GetPort();
        networkTransport.SecureConnection = false;
        networkTransport.AllowForwardedRequest = true;
        networkTransport.CertificateBase64String = string.Empty;

        BytewarsLogger.Log($"Starting server on {networkTransport.ConnectAddress}:{networkTransport.Port}");
        NetworkManager.Singleton.StartServer();
    }
    
    /// <summary>
    /// this is where the magic begin, most variable exist only on IsServer bracket, but not on IsClient
    /// </summary>
    /// <param name="clientNetworkId"></param>
    private void OnClientConnected(ulong clientNetworkId)
    {
        BytewarsLogger.Log($"Client connected. Client id: {clientNetworkId}");

        reconnect.OnClientConnected(clientNetworkId, IsOwner, IsServer, IsClient, IsHost, serverHelper,
                                    InGameMode, connectedClients, InGameState, GameData.ServerType, Players, gameTimeLeft, clientHelper);

        // Broadcast on player enter game event.
        if (IsLocalGame || IsServer) 
        {
            Players.TryGetValue(clientNetworkId, out Player newPlayer);
            if (newPlayer && InGameState is InGameState.Playing) 
            {
                OnPlayerEnterGame?.Invoke(newPlayer.PlayerState);
            }
        }
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        BytewarsLogger.Log($"Client disconnected. Client id: {clientNetworkId}");

        string reason = string.Empty;
        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        bool isInMenuScene = activeSceneBuildIndex == GameConstant.MenuSceneBuildIndex;
        bool isInGameScene = activeSceneBuildIndex == GameConstant.GameSceneBuildIndex;

        if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            reason = NetworkManager.Singleton.DisconnectReason;
        }

        BytewarsLogger.Log(
            $"Client disconnected. Client id: {clientNetworkId}. " +
            $"Reject reason:{reason}. Active entity count: {ActiveGEs.Count}. Is server:{IsServer}");

        if (isInMenuScene)
        {
            if (ClientNetworkId == clientNetworkId)
            {
                OnDisconnectedInMainMenu?.Invoke(reason);
            }

            if (menuManager.IsLoading)
            {
                menuManager.HideLoading();
            }

            if (IsServer)
            {
                RemoveConnectedClient(clientNetworkId, isInGameScene);

                // If host, refresh lobby player entries.
                if (IsHost && menuManager.GetCurrentMenu() is MatchLobbyMenu lobby)
                {
                    lobby.Refresh();
                }

                // Start lobby countdown to shutdown server if no connected clients.
                if (reconnect.IsServerShutdownOnLobby(connectedClients.Count))
                {
                    serverHelper.StartCoroutineCountdown(this,
                        GameData.GameModeSo.LobbyShutdownCountdown, OnLobbyShutdownCountdown);
                }
            }
        }
        else if (isInGameScene)
        {
            if (IsServer && InGameState != InGameState.GameOver)
            {                
                // Player might reconnect in the middle of game, missile will not reset
                RemoveConnectedClient(clientNetworkId, isInGameScene, false);

                // Start the in-game countdown to shut down the server if the required active team is not met.
                if (serverHelper.GetActiveTeamsCount() <= GameData.GameModeSo.MinimumTeamCountToPlay)
                {
                    BytewarsLogger.Log(
                        $"Shutting down due to minimum team not fulfilled. " +
                        $"Current active team count: {serverHelper.GetActiveTeamsCount()}. " +
                        $"Minimum team required: {GameData.GameModeSo.MinimumTeamCountToPlay}");

                    SetInGameState(InGameState.ShuttingDown);
                }
            }
        }

        StartCoroutine(OnClientDisconnectedComplete(clientNetworkId, reason));
    }

    private IEnumerator OnClientDisconnectedComplete(ulong clientNetworkId, string reason)
    {
        BytewarsLogger.Log($"OnClientDisconnectedComplete client id {clientNetworkId} disconnected reason:{reason} " +
          $"active entity count:{ActiveGEs.Count} IsServer:{IsServer}");

        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        bool isInGameScene = activeSceneBuildIndex == GameConstant.GameSceneBuildIndex;

        // Back to the main menu and show the disconnected message (only for client or local network).
        bool isLocalNetwork = ClientNetworkId.Equals(clientNetworkId);
        if (!IsHost && (IsClient || isLocalNetwork))
        {
            bool isInMainMenu = !(isInGameScene || menuManager.GetCurrentMenu() is MatchLobbyMenu);
            if (!isInMainMenu)
            {
                yield return QuitToMainMenu();
            }

            ShowDisconnectedFromServerMessage(reason);
        }
    }

    private void ShowDisconnectedFromServerMessage(string disconnectReason = "")
    {
        if (InGameState == InGameState.GameOver)
        {
            BytewarsLogger.Log("No need to show disconnected from server message if the game state was over.");
            return;
        }

        const string title = "Connection Error";
        string message = "Connection to the server or host has been lost.";
        if (!string.IsNullOrEmpty(disconnectReason))
        {
            message = $"{message}\n\n{disconnectReason}";
        }

        menuManager.ShowInfo(message, title);
    }

    /// <summary>
    /// this is only called in server/hosting
    /// </summary>
    /// <param name="request">client information</param>
    /// <param name="response">set whether the client is allowed to connect or not</param>
    private void ConnectionApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        BytewarsLogger.Log($"Start connection approval for {request.ClientNetworkId}");

        ConnectionApprovalResult result = 
            connectionHelper.ConnectionApproval(request, response, IsServer, InGameState, availableInGameMode, InGameMode, serverHelper);

        BytewarsLogger.Log($"Is {request.ClientNetworkId} connection approved: {response.Approved}. Reason: {response.Reason}");

        if (result == null)
        {
            BytewarsLogger.Log("Failed to handle connection approval result. The result is null.");
            return;
        }

        // Setup initial game data.
        if (InGameMode == InGameMode.None)
        {
            InGameMode = result.InGameMode;
            GameData.GameModeSo = result.GameModeSo;
        }

        // Add reconnect player.
        if (result.ReconnectPlayer != null)
        {
            Players.TryAdd(request.ClientNetworkId, result.ReconnectPlayer);
        }
    }
    
    public void RemoveConnectedClient(ulong clientNetworkId, bool isInGameScene, bool isResetMissile = true)
    {
        BytewarsLogger.LogWarning($"Remove connected client. Client id: {clientNetworkId} is not found.");

        connectedClients.Remove(clientNetworkId);

        if (!Players.TryGetValue(clientNetworkId, out Player player) && isInGameScene)
        {
            BytewarsLogger.LogWarning($"Unable to remove connected client. Player with client id {clientNetworkId} is not found.");
            return;
        }

        if (!IsServer && isInGameScene)
        {
            player.gameObject.SetActive(false);
            Players.Remove(clientNetworkId);
        }

        // If in gameplay, only disconnect player state but don't remove it. This is to support player reconnection.
        if (isInGameScene)
        {
            serverHelper.DisconnectPlayerState(clientNetworkId, player);
        }
        // Else, remove the player state so other player can join the lobby.
        else
        {
            serverHelper.RemovePlayerState(clientNetworkId);
        }

        RemoveConnectedClientRpc(clientNetworkId, serverHelper.ConnectedTeamStates.Values.ToArray(), 
            serverHelper.ConnectedPlayerStates.Values.ToArray(), isResetMissile);
    }

    [ClientRpc]
    private void RemoveConnectedClientRpc(
        ulong clientNetworkId, 
        TeamState[] teamStates,
        PlayerState[] playerStates, 
        bool isResetMissile)
    {
        BytewarsLogger.Log($"[Client] Remove connected client. Client id: {clientNetworkId}. Is host: {IsHost}");

        UpdateInGamePlayerState(teamStates, playerStates);
        connectedClients.Remove(clientNetworkId);

        if (!Players.Remove(clientNetworkId, out Player player))
        {
            BytewarsLogger.LogWarning($"[Client] Unable to remove client. Player with client id {clientNetworkId} is not found");
            return;
        }

        player.Deinitialize(isResetMissile);
    }

    private void ReAddReconnectedPlayerOnClient(
        ulong clientNetworkId, 
        int[] firedMissilesId,
        TeamState[] teamStates, 
        PlayerState[] playerStates,
        LevelCreationResult levelResult)
    {
        BytewarsLogger.Log($"ReAddReconnectedPlayerOnClient IsServer:{IsServer} clientNetworkId:{clientNetworkId}");

        // Reset HUD
        hud.HideGameStatusContainer();

        // Reconnected player may have different network id, thus reassign the client network id to client helper.
        if (clientNetworkId == NetworkManager.Singleton.LocalClientId) 
        {
            clientHelper.SetClientNetworkId(clientNetworkId);
        }

        // Span reconnected player ship.
        Player player = InGameFactory.SpawnReconnectedShip(clientNetworkId, serverHelper, Pool);
        if (player)
        {
            player.SetFiredMissilesId(firedMissilesId);
            Players.TryAdd(clientNetworkId, player);
        }

        // Respawn level objects.
        PlaceLevelObjects(levelResult, teamStates, playerStates);

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
    }

    [ClientRpc]
    public void ReAddReconnectedPlayerClientRpc(
        ulong clientNetworkId, 
        int[] firedMissilesId,
        TeamState[] teamStates, 
        PlayerState[] playerStates,
        LevelCreationResult levelResult)
    {
        BytewarsLogger.Log($"[Client] Re-add reconnected player. Client id: {clientNetworkId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }

        ReAddReconnectedPlayerOnClient(clientNetworkId, firedMissilesId, teamStates, playerStates, levelResult);
    }

    public void OnTransportEvent(NetworkEvent networkEvent, ulong clientNetworkId, ArraySegment<byte> payload, float receiveTime)
    {
        BytewarsLogger.Log($"Received packet. Client id: {clientNetworkId}. Packet count: {payload.Count}. Event type: {networkEvent}. Received time: {receiveTime}");

        // Received event data, but the packet data is empty. This mean the data is corrupted.
        if (networkEvent == NetworkEvent.Data && payload.Count <= 0) 
        {
            BytewarsLogger.Log(
                $"Received corrupted package. Shutting down client. " +
                $"Client id: {clientNetworkId}. Received time: {receiveTime}. Event type: {networkEvent}.");

            NetworkManager.Singleton.DisconnectClient(clientNetworkId, "Network package corrupted. Connection time out.");
        }
    }

    public void SendPingToServer() 
    {
        pingServerRequestTime = Time.realtimeSinceStartup;
        using(FastBufferWriter writer = new FastBufferWriter(sizeof(float), Unity.Collections.Allocator.Temp)) 
        {
            if (writer.TryBeginWrite(sizeof(float))) 
            {
                writer.WriteValue(pingServerRequestTime);
                if (NetworkManager.Singleton.CustomMessagingManager != null) 
                {
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PingNamedMessage, NetworkManager.ServerClientId, writer);
                }
            }
        }
    }

    private void OnReceivePing(ulong clientId, FastBufferReader reader)
    {
        if (!reader.TryBeginRead(sizeof(float)))
        {
            return;
        }

        reader.ReadValue(out float clientTimestamp);
        using (FastBufferWriter writer = new FastBufferWriter(sizeof(float), Unity.Collections.Allocator.Temp))
        {
            if (writer.TryBeginWrite(sizeof(float)))
            {
                writer.WriteValue(clientTimestamp);
                if (NetworkManager.Singleton.CustomMessagingManager != null) 
                {
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PongNamedMessage, clientId, writer);
                }
            }
        }
    }

    private void OnReceivePong(ulong clientId, FastBufferReader reader) 
    {
        if (reader.TryBeginRead(sizeof(float)))
        {
            // Calculate round-time trip in milliseconds.
            reader.ReadValue(out float serverResponseTime);
            CurrentRoundTripTime = (Time.realtimeSinceStartup - serverResponseTime) * 1000;
        }
    }

    #endregion

    #region Game Management

    public void StartGame(GameModeSO gameModeSo)
    {
        if (isGameStarted)
        {
            BytewarsLogger.LogWarning("Cannot start game. Game has already started.");
            return;
        }

        isGameStarted = true;

        GameData.GameModeSo = gameModeSo;
        GameData.ServerType = ServerType.Offline;
        GameMode = gameModeSo.GameMode;
        InGameMode = gameModeSo.InGameMode;

        AudioManager.Instance.PlaySfx("Enter_Simulate");
        ShowTravelingLoading(LoadScene);
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(GameConstant.GameSceneBuildIndex);
    }

    public void RestartLocalGame()
    {
        InGameState = InGameState.None;
        menuManager.CloseInGameMenu();
        ResetLevel();
        SetupGame();
    }

    public IEnumerator QuitToMainMenu()
    {
        if (IsLocalGame || IsServer)
        {
            // Broadcast on-game ended event as abandoned if the host is intentionally quit.
            if (isGameStarted && !isGameEnded && InGameState != InGameState.GameOver) 
            {
                isGameEnded = true;
                OnGameEnded?.Invoke(GameOverReason.Abandoned);
            }

            serverHelper.CancelCountdown();
        }

        yield return reconnect.ClientDisconnectIntentionally();

        if (InGameState == InGameState.LocalPause)
        {
            Time.timeScale = 1f;
        }

        OnClientLeaveSession?.Invoke();

        ResetCache();

        menuManager.CloseInGameMenu();
        menuManager.ChangeToMainMenu();
    }

    public void OnNearHitPlayer(Player nearHitPlayer, Missile missile) 
    {
        nearHitPlayer.PlayerState.NumKilledAttemptInSingleLifetime++;
    }

    public void CheckForGameOverCondition()
    {
        bool isGameOver = serverHelper.IsGameOver();
        bool isOfflineGame = GameMode is GameModeEnum.SinglePlayer or GameModeEnum.LocalMultiplayer;
        bool hasAuthority = IsServer || IsHost || isOfflineGame;

        BytewarsLogger.Log($"Is game over: {isGameOver}");
        if (isGameOver && hasAuthority) 
        {
            BytewarsLogger.Log($"Ending the game.");

            if (IsServer || IsHost) 
            {
                UpdatePlayerStatesClientRpc(
                    serverHelper.ConnectedTeamStates.Values.ToArray(),
                    serverHelper.ConnectedPlayerStates.Values.ToArray());
            }

            SetInGameState(InGameState.GameOver);
        }
    }

    public bool SetInGameState(InGameState newState, bool forceChangeState = false)
    {
        BytewarsLogger.Log($"Try to set in-game state to: {newState}");

        if (!forceChangeState && InGameState == newState)
        {
            BytewarsLogger.LogWarning($"Cannot set in-game state to {newState} because the current state is already the same.");
            return false;
        }

        BytewarsLogger.Log($"In-game state changed from {InGameState} to {newState}");
        InGameState = newState;
        
        // Handle in-game state changes.
        switch (newState)
        {
            case InGameState.None:
                ResetLevel();
#if !UNITY_SERVER
                OnGameStateIsNone?.Invoke();
#endif
                break;
            case InGameState.Initializing:
                gameTimeLeft = GameData.GameModeSo.GameDuration;
                break;
            case InGameState.PreGameCountdown:
                // Start pre-game countdown.
                serverHelper.CancelCountdown();
                serverHelper.StartCoroutineCountdown(
                    this,
                    GameData.GameModeSo.BeforeGameCountdownSecond,
                    OnPreGameTimerUpdated);
                break;
            case InGameState.Playing:
                // Continue game duration countdown.
                serverHelper.CancelCountdown();
                serverHelper.StartCoroutineCountdown(
                    this,
                    gameTimeLeft,
                    OnGameTimeUpdated);
                break;
            case InGameState.ShuttingDown:
                // Start server shutdown countdown.
                serverHelper.CancelCountdown();
                serverHelper.StartCoroutineCountdown(
                    this, 
                    GameData.GameModeSo.BeforeShutDownCountdownSecond, 
                    OnShutdownCountdownUpdate);
                break;
            case InGameState.GameOver:
                // Broadcast to update player states on connected game clients.
                if (IsHost || IsServer)
                {
                    UpdatePlayerStatesClientRpc(
                        serverHelper.ConnectedTeamStates.Values.ToArray(),
                        serverHelper.ConnectedPlayerStates.Values.ToArray());
                }

                isGameEnded = true;
                OnGameEnded?.Invoke(GameOverReason.MatchEnded);

                // Start game over server shutdown countdown.
                bool isShuttingDown = GameData.GameModeSo.GameOverShutdownCountdown > -1;
                serverHelper.CancelCountdown();
                if (!IsLocalGame && isShuttingDown)
                {
                    serverHelper.StartCoroutineCountdown(
                        this, 
                        GameData.GameModeSo.GameOverShutdownCountdown,
                        OnGameOverShutDownCountdown);
                }
                break;
        }

        UpdateInGameUI();

        // Broadcast to update in-game state on connected game clients.
        if (IsServer || IsHost)
        {
            UpdateInGameStateClientRpc(InGameState, gameTimeLeft, forceChangeState);
        }
        
        OnGameStateChanged?.Invoke(newState);

        return true;
    }
    
    [ClientRpc]
    private void UpdateInGameStateClientRpc(InGameState newState, int remainingGameTime, bool forceChangeState = false)
    {
        if (IsHost) 
        {
            BytewarsLogger.LogWarning($"[Client] Unable to set in-game state from {InGameState} to: {newState}. The player is a host, abort to handle client RPC.");
            return;
        }

        BytewarsLogger.Log($"[Client] Try to set in-game state from {InGameState} to: {newState}");
        if (!forceChangeState && InGameState == newState)
        {
            BytewarsLogger.LogWarning($"[Client] Cannot set in-game state from {InGameState} to {newState} because the current state is already the same.");
            return;
        }

        BytewarsLogger.Log($"[Client] In-game state changed from {InGameState} to {newState}");
        InGameState = newState;

        gameTimeLeft = remainingGameTime;
        UpdateInGameUI();
    }

    private void UpdateInGameUI() 
    {
        BytewarsLogger.Log($"Received to update in-game user interface. Current in-game state: {InGameState}. Game time left: {gameTimeLeft}");

        // Adjust time scale when the game is paused or resumed.
        Time.timeScale = InGameState == InGameState.LocalPause ? 0 : 1;

        // Enable the in-game camera behaviors during gameplay.
        if (inGameCamera)
        {
            inGameCamera.enabled = InGameState == InGameState.Playing;
        }

        // Update HUD based on in-game state.
        if (hud && !IsDedicatedServer)
        {
            // Reset game status overlay (the UI to show countdown) by hiding it first.
            hud.HideGameStatusContainer();

            switch (InGameState)
            {
                // Hide HUD during non-gameplay.
                case InGameState.None:
                case InGameState.Initializing:
                case InGameState.LocalPause:
                    hud.gameObject.SetActive(false);
                    break;
                // Keep showing HUD during gameplay.
                case InGameState.PreGameCountdown:
                case InGameState.Playing:
                case InGameState.ShuttingDown:
                    hud.gameObject.SetActive(true);
                    hud.SetTime(gameTimeLeft);
                    break;
                // Hide HUD and show game over when game is over.
                case InGameState.GameOver:
                    if (InGamePause.IsPausing())
                    {
                        InGamePause.ToggleGamePause();
                    }
                    hud.gameObject.SetActive(false);
                    menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);
                    break;
            }
        }
    }

    private void ResetLevel()
    {
        gameTimeLeft = 0;

        foreach (GameEntityAbs ge in ActiveGEs)
        {
            ge.Reset();
        }

        ActiveGEs.Clear();
        Players.Clear();
        planets.Clear();
        hud.Reset();
    }

    public void StartOnlineGame()
    {
        if (!IsServer || !IsOwner)
        {
            BytewarsLogger.LogWarning("Cannot start online game. Instance is not the server or host.");
            return;
        }

        if (isGameStarted)
        {
            BytewarsLogger.LogWarning("Cannot start online game. Game has already started.");
            return;
        }
        
        isGameStarted = true;

        serverHelper.CancelCountdown();
        GameMode = GameData.GameModeSo.GameMode;
        SetGameModeClientRpc(GameMode);

        OnStartingGameClientRpc();
        ShowTravelingLoading(LoadMultiplayerScene, StartingGameMessage);
    }

    private void LoadMultiplayerScene()
    {
        SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(GameConstant.GameSceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            isGameStarted = false;
            isGameEnded = false;
            BytewarsLogger.LogWarning($"Failed to load {GameConstant.GameSceneName} " +
                                      $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }
    
    [ClientRpc]
    private void SetGameModeClientRpc(GameModeEnum gameMode)
    {
        BytewarsLogger.Log($"[Client] Set game mode to {gameMode}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        GameMode = gameMode;
    }
    
    private async void DeregisterServer()
    {
#if UNITY_SERVER
        OnDeregisterServer?.Invoke();
#endif
        await UniTask.Delay(150);
        BytewarsLogger.Log("GameManager Application.Quit");
    }
    
    [ClientRpc]
    private void PlaceObjectsClientRpc(
        LevelCreationResult levelResult,
        TeamState[] teamStates,
        PlayerState[] playerStates)
    {
        BytewarsLogger.Log($"[Client] Place level objects. Is local player: {IsLocalPlayer}. Is client: {IsClient}. Is host: {IsHost}");
        PlaceLevelObjects(levelResult, teamStates, playerStates);
    }
    
    private void PlaceLevelObjects(
        LevelCreationResult levelResult,
        TeamState[] teamStates,
        PlayerState[] playerStates) 
    {
        if (IsHost)
        {
            return;
        }

        if (CreatedLevel != null)
        {
            BytewarsLogger.Log($"[Client] Cannot place level objects. Level objects is already created.");
            return;
        }
        CreatedLevel = levelResult;

        AudioManager.Instance.PlayGameplayBGM();

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);
        availablePositions = new List<Vector3>(levelResult.AvailablePositions);

        clientHelper.PlaceObjectsOnClient(
            levelResult.LevelObjects,
            playerStates.Select(x => x.ClientNetworkId).ToArray(),
            Pool,
            gamePrefabDict,
            planets,
            Players,
            serverHelper,
            ActiveGEs);

        menuManager.HideLoading(false);
        menuManager.CloseMenuPanel();

        hud.gameObject.SetActive(true);
        hud.Init(teamStates, playerStates);
    }

    public void GetWinner(out TeamState winnerTeam, out PlayerState winnerPlayer)
    {
        winnerTeam = null;
        winnerPlayer = null;

        float highestScore = 0;
        foreach (PlayerState playerState in ConnectedPlayerStates.Values)
        {
            // Check for highest score to set the winner.
            if (playerState.Score > highestScore)
            {
                winnerTeam = ConnectedTeamStates.GetValueOrDefault(playerState.TeamIndex);
                winnerPlayer = playerState;
                highestScore = playerState.Score;
            }
            // If score draw, then there is no winner.
            else if (playerState.Score == highestScore)
            {
                winnerTeam = null;
                winnerPlayer = null;
            }
        }
    }

    #endregion

    #region Player Management

    [ClientRpc]
    public void RepositionPlayerClientRpc(
        ulong clientNetworkId,
        Vector3 position,
        int maxInFlightMissile,
        Vector4 teamColor,
        Quaternion rotation)
    {
        BytewarsLogger.Log($"[Client] Reposition player to {position}. Client id {clientNetworkId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        if (!Players.TryGetValue(clientNetworkId, out Player player))
        {
            BytewarsLogger.LogWarning($"[Client] Unable to reposition player. Player with client id {clientNetworkId} is not found");
            return;
        }
        
        player.transform.rotation = rotation;
        player.PlayerState.Position = position;
        player.Initialize(maxInFlightMissile, teamColor);
    }
    
    [ClientRpc]
    public void ResetPlayerClientRpc(ulong clientNetworkId)
    {
        BytewarsLogger.Log($"[Client] Reset player attributes. Client id: {clientNetworkId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        if (!Players.TryGetValue(clientNetworkId, out Player player))
        {
            BytewarsLogger.LogWarning($"[Client] Unable to reset player attributes. Player with client id {clientNetworkId} is not found.");
            return;
        }
        
        player.Reset();
    }
    
    [ClientRpc]
    public void UpdateScoreClientRpc(PlayerState playerState, PlayerState[] playerStates)
    {
        BytewarsLogger.LogWarning($"[Client] Update score. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        hud.UpdateKillsAndScore(playerState, playerStates);
    }
    
    [ClientRpc]
    public void UpdateLiveClientRpc(int teamIndex, int lives)
    {
        BytewarsLogger.LogWarning($"[Client] Update lives. Team index: {teamIndex}. Lives: {lives}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        hud.SetLivesValue(teamIndex, lives);
    }
    
    private void UpdateInGamePlayerState(TeamState[] teamStates, PlayerState[] playerStates)
    {
        BytewarsLogger.Log(
            $"Update in-game player states. " +
            $"Player states: {JsonUtility.ToJson(playerStates)}. Team states: {JsonUtility.ToJson(playerStates)}");
        
        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        if (SceneManager.GetActiveScene().buildIndex == GameConstant.MenuSceneBuildIndex)
        {
            menuManager.HideLoading(false);
            var lobby = (MatchLobbyMenu)menuManager.ChangeToMenu(AssetEnum.MatchLobbyMenuCanvas);
            lobby.Refresh();
        }
    }
    
    [ClientRpc]
    public void UpdatePlayerStatesClientRpc(TeamState[] teamStates, PlayerState[] playerStates)
    {
        BytewarsLogger.Log(
            $"[Client] Update player states. Is host: {IsHost}. " +
            $"Team states: {JsonUtility.ToJson(teamStates)}. Player states: {JsonUtility.ToJson(playerStates)}");

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
    }

    #endregion

    #region Missile Management

    public void OnDestroyMissile(Missile missile, GameObject hitObject)
    {
        BytewarsLogger.Log($"Destroy missile. Hit object: {hitObject?.name}");

        MissileState missileState = missile.MissileState;
        PlayerState owningPlayerState = missile.OwningPlayerState;
        GameEntityDestroyReason destroyReason = GameEntityDestroyReason.Lifetime;

        PlayerState hitPlayerState = null;
        PlanetState hitPlanetState = null;
        
        Player hitPlayer = hitObject?.GetComponent<Player>();
        Planet hitPlanet = hitObject?.GetComponent<Planet>();

        // Trigger on-hit player.
        if (hitPlayer) 
        {
            destroyReason = GameEntityDestroyReason.PlayerHit;
            hitPlayerState = hitPlayer.PlayerState;

            if (IsLocalGame || IsServer) 
            {
                CollisionHelper.OnPlayerHit(hitPlayer, missile, Players, serverHelper, hud, GameMode, availablePositions);
            }

            // Broadcast player die event.
            OnPlayerDie?.Invoke(hitPlayerState, GameEntityDestroyReason.MissileHit, owningPlayerState, missileState);

            // Reset number of kill attempts.
            hitPlayerState.NumKilledAttemptInSingleLifetime = 0;
        }
        // Trigger on-hit planet.
        else if (hitPlanet) 
        {
            destroyReason = GameEntityDestroyReason.PlanetHit;
            hitPlanetState = hitPlanet.PlanetState;
            hitPlanet.OnHitByMissile();
        }

        // Broadcast missile destroyed event.
        OnMissileDestroyed?.Invoke(owningPlayerState, missileState, destroyReason, hitPlanetState, hitPlayerState);
        OnDestroyMissileClientRpc(owningPlayerState, missileState, destroyReason, hitPlanetState, hitPlayerState);
    }

    [ClientRpc]
    public void OnDestroyMissileClientRpc(
        PlayerState owningPlayerState,
        MissileState missileState,
        GameEntityDestroyReason destroyReason,
        PlanetState hitPlanetState,
        PlayerState hitPlayerState)
    {
        ulong clientNetworkId = owningPlayerState.ClientNetworkId;
        BytewarsLogger.Log($"[Client] Desroy missile. Client id: {clientNetworkId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }

        // Parse object hit by missile.
        GameObject hitObject = null;
        if (hitPlayerState != null) 
        {
            hitObject = Players.GetValueOrDefault(hitPlayerState.ClientNetworkId)?.gameObject;
        }
        else if (hitPlanetState != null) 
        {
            Planet hitPlanet = planets.GetValueOrDefault(hitPlanetState.Id);
            hitObject = hitPlanet?.gameObject;

            // If the missile hit a planet, trigger on-planet hit event.
            hitPlanet?.OnHitByMissile();
        }

        // Destroy the missile owned by respective player.
        if (Players.TryGetValue(clientNetworkId, out Player player))
        {
            player.DestroyMissile(missileState, hitObject);
        }
        else
        {
            BytewarsLogger.LogWarning($"[Client] Unable to destroy player missile. Player with client id {clientNetworkId} is not found.");
        }
    }
    
    [ClientRpc]
    public void MissileSyncClientRpc(
        ulong playerClientNetworkId,
        int missileId,
        Vector3 velocity,
        Vector3 position,
        Quaternion rotation)
    {
        BytewarsLogger.Log($"[Client] Missile sync. Client id: {playerClientNetworkId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        if (!Players.TryGetValue(playerClientNetworkId, out Player player)) 
        {
            BytewarsLogger.LogWarning($"[Client] Unable to sync player missile. Player with client id {playerClientNetworkId} is not found.");
            return;
        }
        player.SyncMissile(missileId, velocity, position, rotation);
    }

    #endregion
    
    #region Countdowns and Timers
    
    public void StartShutdownCountdown(int countdown)
    {
        BytewarsLogger.Log($"Start shutdown countdown: {countdown}s");

        serverHelper.StartCoroutineCountdown(this,
            countdown, OnLobbyShutdownCountdown);
    }
    
    private void OnLobbyShutdownCountdown(int countdownSeconds)
    {
        //no player connected no need to update client UI for lobby shutdown
        BytewarsLogger.Log($"Update lobby shutdown countdown: {countdownSeconds}s");
        if (countdownSeconds <= 0)
        {
            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }
    
    public void OnLobbyCountdownServerUpdated(int countdown)
    {
        BytewarsLogger.Log($"Update lobby countdown: {countdown}s");

        UpdateLobbyCountdownClientRpc(countdown);
        if (countdown <= 0)
        {
            StartOnlineGame();
        }
    }
    
    [ClientRpc]
    private void UpdateLobbyCountdownClientRpc(int countdown)
    {
        BytewarsLogger.Log($"[Client] Update lobby countdown: {countdown}s. Client id: {NetworkManager.Singleton.LocalClientId}. Is host: {IsHost}");
        menuManager.UpdateLobbyCountdown(countdown);
    }

    [ClientRpc]
    private void OnStartingGameClientRpc()
    {
        BytewarsLogger.Log($"[Client] Starting game. Client id: {NetworkManager.Singleton.LocalClientId}. Is host: {IsHost}");

        menuManager.CloseMenuPanel();
        ShowTravelingLoading(null, StartingGameMessage);

        AudioManager.Instance.PlaySfx("Enter_Simulate");

        CameraMovement.CancelMoveCameraLerp(mainCamera);
        Vector3 powerBarVisiblePosition = menuManager.TargetCameraPositions.First();
        CameraMovement.MoveCameraLerp(mainCamera, powerBarVisiblePosition, TravelingDelay);
    }

    private void OnPreGameTimerUpdated(int timerSecond)
    {
        BytewarsLogger.Log($"Update pre-game countdown: {timerSecond}s");

        hud.UpdatePreGameCountdown(timerSecond);

        if (timerSecond == 0)
        {
            bool isEnoughPlayers = IsLocalGame ? true : serverHelper.GetActiveTeamsCount() > GameData.GameModeSo.MinimumTeamCountToPlay;
            BytewarsLogger.Log($"On pre-game countdown over. Is enough players: {isEnoughPlayers}");

            // If local game, simply start the game. Otherwise, check if there are enough players.
            InGameState newState = isEnoughPlayers ? InGameState.Playing : InGameState.ShuttingDown;
            SetInGameState(newState);

            // Broadcast on-game started.
            OnGameStarted?.Invoke();

            // Broadcast on-player enter game event.
            foreach (Player player in Players.Values)
            {
                if (player == null)
                {
                    continue;
                }

                OnPlayerEnterGame?.Invoke(player.PlayerState);
            }
        }

        if (!IsLocalGame)
        {
            UpdatePreGameCountdownClientRpc(timerSecond);
        }
    }
    
    [ClientRpc]
    private void UpdatePreGameCountdownClientRpc(int second)
    {
        BytewarsLogger.Log($"[Client] Update pre-game countdown: {second}s. Client id: {NetworkManager.Singleton.LocalClientId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        hud.UpdatePreGameCountdown(second);
    }

    private void OnGameTimeUpdated(int remainingGameTime)
    {
        BytewarsLogger.Log($"Update game time: {remainingGameTime}s");

        if (InGameState != InGameState.Playing)
        {
            BytewarsLogger.LogWarning($"Unable to handle update game time event. In-game state is not in {InGameState.Playing} state");
            return;
        }

        gameTimeLeft = remainingGameTime;
        UpdateInGameUI();

        UpdateGameTimeClientRpc(remainingGameTime);
        
        if (remainingGameTime <= 0)
        {
            SetInGameState(InGameState.GameOver);
        }
    }
    
    [ClientRpc]
    private void UpdateGameTimeClientRpc(int remainingGameTime)
    {
        BytewarsLogger.Log($"[Client] Update game time: {remainingGameTime}s");

        if (IsHost)
        {
            BytewarsLogger.LogWarning($"[Client] Unable to handle update game time event. The player is a host, abort to handle client RPC.");
            return;
        }

        gameTimeLeft = remainingGameTime;
        UpdateInGameUI();
    }
    
    private void OnGameOverShutDownCountdown(int countdownSeconds)
    {
        BytewarsLogger.Log($"Update game over shutdown countdown: {countdownSeconds}");

        menuManager.UpdateGameOverCountdown(countdownSeconds);
        GameOverCountdownClientRpc(countdownSeconds);
        
        bool shuttingDown = countdownSeconds <= 0;
        if (shuttingDown)
        {
            StartCoroutine(QuitToMainMenu());
        }
    }
    
    [ClientRpc]
    private void GameOverCountdownClientRpc(int seconds)
    {
        BytewarsLogger.Log($"[Client] Update game over countdown: {seconds}s. Client id: {NetworkManager.Singleton.LocalClientId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        menuManager.UpdateGameOverCountdown(seconds);
    }
    
    private void OnShutdownCountdownUpdate(int countdownSecond)
    {
        BytewarsLogger.Log($"Update shutdown countdown: {countdownSecond}s");

        hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        
        if (IsServer)
        {
            ShutdownClientRpc(countdownSecond);
        }
        
        bool willShutdown = countdownSecond <= 0;
        if (willShutdown)
        {
            // Broadcast on-game ended event as abandoned when the game shutdown.
            if (!isGameEnded && InGameState != InGameState.GameOver)
            {
                isGameEnded = true;
                OnGameEnded?.Invoke(GameOverReason.Abandoned);
            }

            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }
    
    [ClientRpc]
    private void ShutdownClientRpc(int countdownSecond)
    {
        BytewarsLogger.Log($"[Client] Update shutdown countdown: {countdownSecond}s. Client id: {NetworkManager.Singleton.LocalClientId}. Is host: {IsHost}");

        if (IsHost)
        {
            return;
        }
        
        hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        if (countdownSecond <= 0)
        {
            StartCoroutine(QuitToMainMenu());
        }
    }
    
    #endregion
    
    #region Scene Management
    
    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        BytewarsLogger.Log($"Active scene changed. Current scene: {current.name}:{current.buildIndex}. Next scene: {next.name}:{next.buildIndex}");

#if UNITY_SERVER
        switch(next.buildIndex) 
        {
            case GameConstant.GameSceneBuildIndex:
                // Setup game.
                if (InGameState == InGameState.None)
                {
                    BytewarsLogger.Log("Game secene loaded. Setup game.");
                    OnRejectBackfill?.Invoke();
                    menuManager.CloseMenuPanel();
                    Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);
                    SetupGame();
                }
                break;
            case GameConstant.MenuSceneBuildIndex:
                // Shutdown server when game ends (e.g. when back to Main Menu).
                BytewarsLogger.Log("Server returned to main menu. Shutting down.");
                NetworkManager.Singleton.Shutdown();
                DeregisterServer();
                return;
        }
#else
        switch (next.buildIndex)
        {
            case GameConstant.GameSceneBuildIndex:
                AudioManager.Instance.PlayGameplayBGM();
                menuManager.HideLoading();
                menuManager.CloseMenuPanel();
                SetupGame();
                break;
            case GameConstant.MenuSceneBuildIndex:
                SetInGameState(InGameState.None);
                hud.gameObject.SetActive(false);
                ResetLevel();
                Pool?.ResetAll();
                break;
        }
#endif

        if (!IsDedicatedServer) 
        {
            OnClientActiveSceneChangedServerRpc(NetworkManager.Singleton.LocalClientId, current.buildIndex, next.buildIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnClientActiveSceneChangedServerRpc(ulong clientNetworkId, int currentSceneBuildIndex, int nextSceneBuildIndex) 
    {
        // Start game when all clients are in the game scene.
        if (nextSceneBuildIndex == GameConstant.GameSceneBuildIndex) 
        {
            if (connectedClients.Keys.Contains(clientNetworkId))
            {
                clientsInGame.Add(clientNetworkId);
            }

            if (connectedClients.Count == clientsInGame.Count)
            {
                BytewarsLogger.Log("All clients are loaded. Starting game.");
                SetInGameState(InGameState.PreGameCountdown);
            }
        }
    }
    
    public void ShowTravelingLoading(Action onComplete, string loadingMessage = "")
    {
        if (string.IsNullOrEmpty(loadingMessage)) 
        {
            loadingMessage = TravelingMessage;
        }

        Coroutine travelingCoroutine = StartCoroutine(OnShowTravelingLoading(onComplete));

        // Show traveling loading with time out countdown.
        if (!IsDedicatedServer) 
        {
            MenuManager.Instance.ShowLoading(
                loadingMessage,
                new LoadingTimeoutInfo()
                {
                    Info = "Time out in ",
                    TimeoutReachedError = "Failed to travel to the the server or host. Connection time out.",
                    TimeoutSec = TravelingTimeOut
                },
                () =>
                {
                    BytewarsLogger.LogWarning($"Shutting down due to traveling time out");

                    if (travelingCoroutine != null)
                    {
                        StopCoroutine(travelingCoroutine);
                    }

                    NetworkManager.Singleton.Shutdown();
                },
                false);
        }
    }

    private IEnumerator OnShowTravelingLoading(Action onComplete) 
    {
        yield return new WaitForSeconds(TravelingDelay);
        onComplete?.Invoke();
    }

    #endregion

    #region Network Management

    public override void OnNetworkSpawn()
    {
        BytewarsLogger.Log($"On-network object spawn. Object id: {NetworkObjectId}");

        if (IsOwner && !IsServer)
        {
            ClientConnectedServerRpc(NetworkObjectId);
        }
        base.OnNetworkSpawn();
    }
    
    public void StartAsClient(string address, ushort port, InGameMode inGameMode, string sessionId = null)
    {
        // Start connect the client to game server.
        InitialConnectionData initialData = new InitialConnectionData
        {
            inGameMode = inGameMode, 
            sessionId = sessionId
        };
        reconnect.ConnectAsClient(networkTransport, address, port, initialData);
    }
    
    public void StartAsHost(string address, ushort port, InGameMode inGameMode, string serverSessionId)
    {
        
        var initialData = new InitialConnectionData()
            { inGameMode = inGameMode, serverSessionId = serverSessionId };
        reconnect.StartAsHost(networkTransport, address, port, initialData);
    }
    
    /// <summary>
    /// called by server to notify client that the playerstate and teamstate for the player are updated
    /// </summary>
    /// <param name="teamStates"></param>
    /// <param name="playerStates"></param>
    /// <param name="inGameMode"></param>
    /// <param name="isInGameScene"></param>
    [ClientRpc]
    public void SendConnectedPlayerStateClientRpc(
        TeamState[] teamStates, 
        PlayerState[] playerStates,
        InGameMode inGameMode,
        ServerType serverType,
        bool isInGameScene)
    {
        BytewarsLogger.Log(
            $"[Client] Send connected player state. Is host: {IsHost}. " +
            $"Player states: {JsonUtility.ToJson(playerStates)}. Team states: {JsonUtility.ToJson(teamStates)}");

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        InGameMode = inGameMode;
        GameData.GameModeSo = availableInGameMode.FirstOrDefault(x => x.InGameMode == InGameMode);
        GameData.ServerType = serverType;
        GameData.CachedPlayerState = playerStates.FirstOrDefault(x => x.ClientNetworkId == NetworkManager.Singleton.LocalClientId);

        if (!isInGameScene)
        {
            menuManager.HideLoading(false);
            var lobby = (MatchLobbyMenu)menuManager.ChangeToMenu(AssetEnum.MatchLobbyMenuCanvas);
            lobby.Refresh();
        }
    }
    
    [ServerRpc]
    private void ClientConnectedServerRpc(ulong networkObjectId)
    {
        BytewarsLogger.Log($"ClientConnectedServerRpc IsServer{IsServer} networkObjectId:{networkObjectId}");
    }
    
    public void StartAsClient(string address, ushort port, InitialConnectionData initialConnectionData)
    {
        reconnect.ConnectAsClient(networkTransport, address, port, initialConnectionData);
    }

    #endregion
}