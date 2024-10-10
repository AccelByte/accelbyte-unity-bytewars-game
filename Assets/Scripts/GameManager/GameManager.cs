// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InGameHUD hud;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameEntityAbs[] gamePrefabs;
    [SerializeField] private FxEntity[] fxPrefabs;
    [SerializeField] private GameModeSO[] availableInGameMode;
    [SerializeField] private Reconnect reconnect;
    [SerializeField] private Transform container;
    [SerializeField] private InGameCamera inGameCamera;
    
    public static GameManager Instance { get; private set; }
    
    public delegate void GameOverDelegate(GameModeEnum gameMode, InGameMode inGameMode, List<PlayerState> playerStates);
    public static event GameOverDelegate OnGameOver = delegate {};
    public static event Action<string> OnDisconnectedInMainMenu;
    public static event Action<InGameState> OnGameStateChanged;
    public static event Action<PlayerState /*deathPlayer*/, PlayerState /*killer*/> OnPlayerDie = delegate { };

    public event Action OnClientLeaveSession;
    public event Action OnDeregisterServer;
    public event Action OnRegisterServer;
    public event Action OnRejectBackfill;
    public event Action OnGameStateIsNone;

    public InGameState InGameState { get; private set; } = InGameState.None;
    public InGameMode InGameMode { get; private set; } = InGameMode.None;
    public InGamePause InGamePause { get; private set; }
    public List<GameEntityAbs> ActiveGEs { get; } = new();
    public Dictionary<ulong, Player> Players { get; } = new();
    public ObjectPooling Pool { get; private set; }
    public Camera MainCamera => mainCamera;
    public TeamState[] TeamStates => serverHelper.GetTeamStates();
    public Dictionary<int, TeamState> ConnectedTeamStates => serverHelper.ConnectedTeamStates;
    public Dictionary<ulong, PlayerState> ConnectedPlayerStates => serverHelper.ConnectedPlayerStates;
    public ulong ClientNetworkId => clientHelper.ClientNetworkId;
    
    private const string NotEnoughPlayer = "Not enough players, shutting down DS in: ";
    
    private readonly Dictionary<ulong, GameClientController> connectedClients = new();
    private readonly Dictionary<string, GameEntityAbs> gamePrefabDict = new();
    private readonly Dictionary<int, Planet> planets = new();
    private readonly ConnectionHelper connectionHelper = new();
    private readonly ServerHelper serverHelper = new();
    private readonly ClientHelper clientHelper = new();
    
    private GameModeEnum gameMode = GameModeEnum.MainMenu;
    private List<Vector3> availablePositions;
    private UnityTransport unityTransport;
    private DebugImplementation debug;
    private MenuManager menuManager;
    private int gameTimeLeft;

    public static readonly int TravelingDelay = 1;
    private bool isGameStarted = false;

    public bool IsDedicatedServer { get { return IsServer && !IsHost && !IsClient; } }

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

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        if (unityTransport == null)
        {
            unityTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        }

#if UNITY_SERVER
        StartServer();
#endif

        debug ??= new DebugImplementation();
        hud.Reset();
        
        InitMenuManagerWhenReady().ContinueWith(_ =>
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
    
    private async Task InitMenuManagerWhenReady()
    {
        while (!MenuManager.Instance.IsInitiated)
        {
            await Task.Delay(50);
        }
        
        menuManager = MenuManager.Instance;
        menuManager.SetEventSystem(eventSystem);
    }

    private void SetupGame()
    {
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

        Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);

        ActiveGEs.RemoveAll(ge => !ge);
        Players.Clear();
        
        if (gameMode == GameModeEnum.OnlineMultiplayer)
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
        var states = InGameFactory.CreateLocalGameState(GameData.GameModeSo);
        
        serverHelper.SetTeamAndPlayerState(states);
        
        CreateLevelAndInitializeHud(states.m_teamStates, states.m_playerStates, out _);
        
        bool isSinglePlayer = gameMode == GameModeEnum.SinglePlayer;
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
        
        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Init(serverHelper.ConnectedTeamStates.Values.ToArray(),
                      serverHelper.ConnectedPlayerStates.Values.ToArray());
        }
        
        CreateLevelAndInitializeHud(serverHelper.ConnectedTeamStates,
                                    serverHelper.ConnectedPlayerStates,
                                    out CreateLevelResult result);
        
        PlaceObjectsClientRpc(result.LevelObjects, Players.Keys.ToArray(),
                              result.AvailablePositions.ToArray(),
                              serverHelper.ConnectedTeamStates.Values.ToArray(),
                              serverHelper.ConnectedPlayerStates.Values.ToArray());
    }
    
    private void CreateLevelAndInitializeHud(Dictionary<int, TeamState> teamStates,
                                             Dictionary<ulong, PlayerState> playerStates,
                                             out CreateLevelResult result)
    {
        result = InGameFactory.CreateLevel(GameData.GameModeSo, ActiveGEs, Players, Pool, teamStates, playerStates);
        availablePositions = result.AvailablePositions;
        
        if (hud == null)
        {
            return;
        }
        
        hud.gameObject.SetActive(true);
        hud.Init(teamStates.Values.ToArray(), playerStates.Values.ToArray());
    }
    
    public void ResetCache()
    {
        isGameStarted = false;
        gameMode = GameModeEnum.MainMenu;
        InGameMode = InGameMode.None;
        connectedClients.Clear();
        serverHelper.Reset();
    }

    #endregion
    
    #region Connection Management

    private void OnServerStopped(bool isHostStopped)
    {
        if (isHostStopped)
        {
            BytewarsLogger.Log("Hosting server has stopped");
            InGameMode = InGameMode.None;
        }

        ResetCache();
    }

    private void OnClientStopped(bool isHostStopped)
    {
        if (isHostStopped) 
        {
            ResetCache();
        }
        
        reconnect.OnClientStopped(isHostStopped, InGameState, serverHelper,
            clientHelper.ClientNetworkId, InGameMode);
    }

    private void StartServer()
    {
        if (!unityTransport)
        {
            return;
        }

        GameData.ServerType = ServerType.OnlineDedicatedServer;

        unityTransport.ConnectionData.Address = ConnectionHandler.GetLocalIPAddress();
        unityTransport.ConnectionData.Port = ConnectionHandler.GetPort();
        unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";

        NetworkManager.Singleton.StartServer();
        NetworkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;

        BytewarsLogger.Log("Server Address: " + unityTransport.ConnectionData.ServerListenAddress.ToString());
        BytewarsLogger.Log("Server Port: " + unityTransport.ConnectionData.Port.ToString());
        BytewarsLogger.Log("server started");
    }
    
    /// <summary>
    /// this is where the magic begin, most variable exist only on IsServer bracket, but not on IsClient
    /// </summary>
    /// <param name="clientNetworkId"></param>
    private void OnClientConnected(ulong clientNetworkId)
    {
        reconnect.OnClientConnected(clientNetworkId, IsOwner, IsServer, IsClient, IsHost, serverHelper,
                                    InGameMode, connectedClients, InGameState, GameData.ServerType, Players, gameTimeLeft, clientHelper);
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        string reason = string.Empty;
        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        bool isInMenuScene = activeSceneBuildIndex == GameConstant.MenuSceneBuildIndex;
        bool isInGameScene = activeSceneBuildIndex == GameConstant.GameSceneBuildIndex;

        if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            reason = NetworkManager.Singleton.DisconnectReason;
        }

        BytewarsLogger.Log($"OnClientDisconnected client id {clientNetworkId} disconnected reason:{reason} " +
                  $"active entity count:{ActiveGEs.Count} IsServer:{IsServer}");

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
                        GameData.GameModeSo.lobbyShutdownCountdown, OnLobbyShutdownCountdown);
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
                if (serverHelper.GetActiveTeamsCount() <= GameData.GameModeSo.minimumTeamCountToPlay)
                {
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
    private async void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request,
                                                  NetworkManager.ConnectionApprovalResponse response)
    {
        ConnectionApprovalResult result = await connectionHelper.ConnectionApproval(request, response, IsServer,
                                              InGameState, availableInGameMode, InGameMode, serverHelper);
        if (result == null)
        {
            return;
        }
        
        if (InGameMode == InGameMode.None)
        {
            InGameMode = result.InGameMode;
            GameData.GameModeSo = result.GameModeSo;
        }
        
        if (result.reconnectPlayer != null)
        {
            Players.TryAdd(request.ClientNetworkId, result.reconnectPlayer);
        }
    }
    
    public void RemoveConnectedClient(ulong clientNetworkId, bool isInGameScene, bool isResetMissile = true)
    {
        connectedClients.Remove(clientNetworkId);

        if (!Players.TryGetValue(clientNetworkId, out Player player) && isInGameScene)
        {
            return;
        }

        if (!IsServer && isInGameScene)
        {
            player.gameObject.SetActive(false);
            Players.Remove(clientNetworkId);
        }

        if (!isInGameScene || InGameState == InGameState.GameOver)
        {
            serverHelper.RemovePlayerState(clientNetworkId);
        }
        else
        {
            serverHelper.DisconnectPlayerState(clientNetworkId, player);
        }

        RemoveConnectedClientRpc(clientNetworkId, serverHelper.ConnectedTeamStates.Values.ToArray(), 
            serverHelper.ConnectedPlayerStates.Values.ToArray(), isResetMissile);
    }

    [ClientRpc]
    private void RemoveConnectedClientRpc(ulong clientNetworkId, TeamState[] teamStates,
        PlayerState[] playerStates, bool isResetMissile)
    {
        UpdateInGamePlayerState(teamStates, playerStates);
        connectedClients.Remove(clientNetworkId);

        if (!Players.Remove(clientNetworkId, out Player player))
        {
            return;
        }

        if (isResetMissile)
        {
            player.Reset();
        }
        else
        {
            player.gameObject.SetActive(false);
        }
    }

    private void ReAddReconnectedPlayerOnClient(ulong clientNetworkId, int[] firedMissilesId,
        TeamState[] teamStates, PlayerState[] playerStates)
    {
        BytewarsLogger.Log($"ReAddReconnectedPlayerOnClient IsServer:{IsServer} clientNetworkId:{clientNetworkId}");

        clientHelper.SetClientNetworkId(clientNetworkId);
        hud.HideGameStatusContainer();

        Player player = InGameFactory.SpawnReconnectedShip(clientNetworkId, serverHelper, Pool);
        if (player)
        {
            player.SetFiredMissilesId(firedMissilesId);
            Players.TryAdd(clientNetworkId, player);
        }

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
    }

    [ClientRpc]
    public void ReAddReconnectedPlayerClientRpc(ulong clientNetworkId, int[] firedMissilesId,
        TeamState[] teamStates, PlayerState[] playerStates)
    {
        if (IsHost)
        {
            return;
        }

        ReAddReconnectedPlayerOnClient(clientNetworkId, firedMissilesId, teamStates, playerStates);
    }

    #endregion

    #region Game Management

    public async void StartGame(GameModeSO gameModeSo)
    {
        if (isGameStarted)
        {
            BytewarsLogger.LogWarning("Cannot start game. Game has already started.");
            return;
        }

        isGameStarted = true;

        GameData.GameModeSo = gameModeSo;
        GameData.ServerType = ServerType.Offline;
        gameMode = gameModeSo.gameMode;
        InGameMode = GetEnumFromGameMode(gameModeSo);

        AudioManager.Instance.PlaySfx("Enter_Simulate");

        await ShowTravelingLoading();

        SceneManager.LoadScene(GameConstant.GameSceneBuildIndex);
    }

    private InGameMode GetEnumFromGameMode(GameModeSO gameModeSo)
    {
        return (InGameMode)Enum.Parse(typeof(InGameMode), gameModeSo.name);
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
        if (IsServer)
        {
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

    public void OnObjectHit(Player player, Missile missile)
    {
        CollisionHelper.OnObjectHit(player, missile, Players, serverHelper,
            hud, this, gameMode, availablePositions);

        // Broadcast on-player die event.
        OnPlayerDie?.Invoke(player.PlayerState, missile.GetOwningPlayerState());
        OnPlayerDieClientRpc(player.PlayerState, missile.GetOwningPlayerState());
        player.PlayerState.numKilledAttemptInSingleLifetime = 0;
    }

    public void OnNearHitPlayer(Player nearHitPlayer, Missile missile) 
    {
        nearHitPlayer.PlayerState.numKilledAttemptInSingleLifetime++;
    }

    [ClientRpc]
    private void OnPlayerDieClientRpc(PlayerState deathPlayer, PlayerState killer)
    {
        if (!IsHost)
        {
            OnPlayerDie?.Invoke(deathPlayer, killer);
        }
    }

    public void CheckForGameOverCondition(bool isGameOver)
    {
        bool isOfflineGame = gameMode is GameModeEnum.SinglePlayer or GameModeEnum.LocalMultiplayer;

        if (isOfflineGame && isGameOver)
        {
            EndGame();
        }
        else if ((NetworkManager.Singleton.IsServer || IsHost) && isGameOver)
        {
            UpdatePlayerStatesClientRpc(
                serverHelper.ConnectedTeamStates.Values.ToArray(),
                serverHelper.ConnectedPlayerStates.Values.ToArray());
            //update to client
            EndGame();
        }
    }

    private void EndGame()
    {
        SetInGameState(InGameState.GameOver);
    }

    public bool SetInGameState(InGameState state)
    {
        BytewarsLogger.Log("try SetInGameState: "+state);
        if (InGameState == state)
        {
            return false;
        }

        if (!IsDedicatedServer && hud)
        {
            hud.HideGameStatusContainer();
        }

        int remainingGameDuration = GameData.GameModeSo.gameDuration;
        if (gameTimeLeft > 0)
        {
            remainingGameDuration = gameTimeLeft;
        }

        InGameState = state;
        switch (state)
        {
            case InGameState.None:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(false);
                }
                ResetLevel();
#if !UNITY_SERVER
                OnGameStateIsNone?.Invoke();
#endif
                break;

            case InGameState.Initializing:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(false);
                }
                break;

            case InGameState.PreGameCountdown:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(true);
                    hud.SetTime(remainingGameDuration);
                }

                serverHelper.CancelCountdown();
                
                if (this)
                {
                    serverHelper.StartCoroutineCountdown(this, 
                        GameData.GameModeSo.beforeGameCountdownSecond, 
                        OnPreGameTimerUpdated);
                }
                break;

            case InGameState.Playing:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(true);
                }
                
                serverHelper.CancelCountdown();
                serverHelper.StartCoroutineCountdown(this, 
                    remainingGameDuration,
                    OnGameTimeUpdated);
                break;

            case InGameState.ShuttingDown:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(true);
                }
                
                serverHelper.CancelCountdown();
                serverHelper.StartCoroutineCountdown(this, 
                    GameData.GameModeSo.beforeShutDownCountdownSecond, 
                    OnShutdownCountdownUpdate);
                break;

            case InGameState.LocalPause:
                if (!IsDedicatedServer && hud)
                {
                    hud.gameObject.SetActive(false);
                }
                break;

            case InGameState.GameOver:
                OnGameOver.Invoke(gameMode, InGameMode, ConnectedPlayerStates.Values.ToList());
                serverHelper.CancelCountdown();

                bool isShuttingDown = GameData.GameModeSo.gameOverShutdownCountdown > -1;
                if (!IsLocalGame() && isShuttingDown)
                {
                    serverHelper.StartCoroutineCountdown(this, 
                        GameData.GameModeSo.gameOverShutdownCountdown,
                        OnGameOverShutDownCountdown);
                }

                if (IsHost || IsServer)
                {
                    UpdatePlayerStatesClientRpc(
                        serverHelper.ConnectedTeamStates.Values.ToArray(),
                        serverHelper.ConnectedPlayerStates.Values.ToArray());
                }

                if (!IsDedicatedServer)
                {
                    if (InGamePause.IsPausing())
                    {
                        InGamePause.ToggleGamePause();
                    }

                    hud.gameObject.SetActive(false);
                    menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);
                }
                break;
        }
        
        if (state == InGameState.LocalPause)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }

        if (inGameCamera)
        {
            inGameCamera.enabled = state == InGameState.Playing;
        }

        if (IsServer || IsHost)
        {
            UpdateInGameStateClientRpc(InGameState, remainingGameDuration);
        }

        OnGameStateChanged?.Invoke(state);

        return true;
    }
    
    [ClientRpc]
    private void UpdateInGameStateClientRpc(InGameState inGameState, int remainingGameTime)
    {
        BytewarsLogger.Log($"change state to {inGameState}");
        
        if (hud)
        {
            hud.HideGameStatusContainer();
        }

        InGameState = inGameState;
        if (inGameState == InGameState.Initializing)
        {
            hud.SetTime(remainingGameTime);
        }
        if (inGameState == InGameState.GameOver)
        {
            if (InGamePause.IsPausing())
            {
                InGamePause.ToggleGamePause();
            }

            hud.gameObject.SetActive(false);
            menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);
        }
        
        inGameCamera.enabled = InGameState == InGameState.Playing;
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
    
    public async void StartOnlineGame()
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
        gameMode = GameData.GameModeSo.gameMode;
        SetGameModeClientRpc(gameMode);

        OnStartingGameClientRpc();

        await Task.Delay(TimeSpan.FromSeconds(TravelingDelay));

        SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(GameConstant.GameSceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            isGameStarted = false;
            BytewarsLogger.LogWarning($"Failed to load {GameConstant.GameSceneName} " +
                                      $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }
    
    [ClientRpc]
    private void SetGameModeClientRpc(GameModeEnum gameMode)
    {
        if (IsHost)
        {
            return;
        }
        
        this.gameMode = gameMode;
    }
    
    private async void DeregisterServer()
    {
#if UNITY_SERVER
        OnDeregisterServer?.Invoke();
#endif
        await Task.Delay(150);
        BytewarsLogger.Log("GameManager Application.Quit");
    }
    
    [ClientRpc]
    private void PlaceObjectsClientRpc(LevelObject[] levelObjects, ulong[] playersClientIds,
                                       Vector3[] availablePositionsP, TeamState[] teamStates, PlayerState[] playerStates)
    {
        BytewarsLogger.Log($"PlaceObjectsClientRpc IsLocalPlayer:{IsLocalPlayer} IsClient:{IsClient} IsHost:{IsHost}");
        if (IsHost)
        {
            return;
        }
        
        AudioManager.Instance.PlayGameplayBGM();

        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);
        availablePositions = new List<Vector3>(availablePositionsP);
        
        clientHelper.PlaceObjectsOnClient(levelObjects, playersClientIds, Pool,
                                           gamePrefabDict, planets, Players, serverHelper, ActiveGEs);

        menuManager.HideLoading(false);
        menuManager.CloseMenuPanel();
        hud.gameObject.SetActive(true);
        hud.Init(teamStates, playerStates);
    }
    
    public static bool IsLocalGame() => !NetworkManager.Singleton.IsListening;

    #endregion

    #region Player Management
    
    [ClientRpc]
    public void RepositionPlayerClientRpc(ulong clientNetworkId, Vector3 position,
                                          int maxInFlightMissile, Vector4 teamColor, Quaternion rotation)
    {
        if (IsHost)
        {
            return;
        }
        
        if (!Players.TryGetValue(clientNetworkId, out Player player))
        {
            return;
        }
        
        player.Reset();
        player.transform.rotation = rotation;
        player.PlayerState.position = position;
        player.Init(maxInFlightMissile, teamColor);
    }
    
    [ClientRpc]
    public void ResetPlayerClientRpc(ulong clientNetworkId)
    {
        if (IsHost)
        {
            return;
        }
        
        if (!Players.TryGetValue(clientNetworkId, out Player player))
        {
            return;
        }
        
        player.Reset();
    }
    
    [ClientRpc]
    public void UpdateScoreClientRpc(PlayerState playerState, PlayerState[] playerStates)
    {
        if (IsHost)
        {
            return;
        }
        
        hud.UpdateKillsAndScore(playerState, playerStates);
    }
    
    [ClientRpc]
    public void UpdateLiveClientRpc(int teamIndex, int lives)
    {
        if (IsHost)
        {
            return;
        }
        
        hud.SetLivesValue(teamIndex, lives);
    }
    
    private void UpdateInGamePlayerState(TeamState[] teamStates, PlayerState[] playerStates)
    {
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
        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        //TODO update game clients UI
    }

    #endregion

    #region Missile Management
    
    [ClientRpc]
    public void MissileHitClientRpc(ulong playerClientNetworkId, int missileId, int planetId, 
                                    Vector3 missileExpPos, Quaternion missileExpRot)
    {
        if (IsHost)
        {
            return;
        }
        
        if (Players.TryGetValue(playerClientNetworkId, out Player player))
        {
            player.ExplodeMissile(missileId, missileExpPos, missileExpRot);
        }
        
        if (planetId > -1 && planets.TryGetValue(planetId, out Planet planet))
        {
            planet.OnHitByMissile();
        }
    }
    
    [ClientRpc]
    public void MissileSyncClientRpc(ulong playerClientNetworkId, int missileId, Vector3 velocity,
                                     Vector3 position, Quaternion rotation)
    {
        if (IsHost)
        {
            return;
        }
        
        if (Players.TryGetValue(playerClientNetworkId, out Player player))
        {
            player.SyncMissile(missileId, velocity, position, rotation);
        }
    }

    #endregion
    
    #region Countdowns and Timers
    
    public void StartShutdownCountdown(int countdown)
    {
        serverHelper.StartCoroutineCountdown(this,
            countdown, OnLobbyShutdownCountdown);

    }
    
    private void OnLobbyShutdownCountdown(int countdownSeconds)
    {
        //no player connected no need to update client UI for lobby shutdown
        BytewarsLogger.Log($"OnLobbyShutdownCountdown countdown:{countdownSeconds}");
        if (countdownSeconds <= 0)
        {
            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }
    
    public void OnLobbyCountdownServerUpdated(int countdown)
    {
        UpdateLobbyCountdownClientRpc(countdown);
        if (countdown <= 0)
        {
            StartOnlineGame();
        }
    }
    
    [ClientRpc]
    private void UpdateLobbyCountdownClientRpc(int countdown)
    {
        menuManager.UpdateLobbyCountdown(countdown);
    }

    [ClientRpc]
    private void OnStartingGameClientRpc()
    {
        menuManager.CloseMenuPanel();
        menuManager.ShowLoading("Starting Game");

        AudioManager.Instance.PlaySfx("Enter_Simulate");

        CameraMovement.CancelMoveCameraLerp(mainCamera);
        Vector3 powerBarVisiblePosition = menuManager.TargetCameraPositions.First();
        CameraMovement.MoveCameraLerp(mainCamera, powerBarVisiblePosition, TravelingDelay);
    }

    private void OnPreGameTimerUpdated(int timerSecond)
    {
        hud.UpdatePreGameCountdown(timerSecond);
        bool areEnoughPlayersConnected = 
            !IsLocalGame() && 
            serverHelper.GetActiveTeamsCount() >= GameData.GameModeSo.minimumTeamCountToPlay;
        
        if (timerSecond == 0)
        {
            if (IsLocalGame())
            {
                SetInGameState(InGameState.Playing);
                return;
            }
            
            SetInGameState(areEnoughPlayersConnected ? InGameState.Playing : InGameState.ShuttingDown);
        }
        
        if (!IsLocalGame())
        {
            UpdatePreGameCountdownClientRpc(timerSecond);
        }
    }
    
    [ClientRpc]
    private void UpdatePreGameCountdownClientRpc(int second)
    {
        if (IsHost)
        {
            return;
        }
        
        hud.UpdatePreGameCountdown(second);
    }

    private void OnGameTimeUpdated(int remainingTime)
    {
        if (InGameState != InGameState.Playing)
        {
            return;
        }
        
        gameTimeLeft = remainingTime;
        hud.SetTime(remainingTime);
        UpdateGameTimeClientRpc(remainingTime);
        
        if (remainingTime <= 0)
        {
            SetInGameState(InGameState.GameOver);
        }
    }
    
    [ClientRpc]
    private void UpdateGameTimeClientRpc(int remainingTimeSecond)
    {
        if (IsHost)
        {
            return;
        }
        
        hud.SetTime(remainingTimeSecond);
    }
    
    private void OnGameOverShutDownCountdown(int countdownSeconds)
    {
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
        if (IsHost)
        {
            return;
        }
        
        menuManager.UpdateGameOverCountdown(seconds);
    }
    
    private void OnShutdownCountdownUpdate(int countdownSecond)
    {
        BytewarsLogger.Log("shutdown in " + countdownSecond);
        hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        
        if (IsServer)
        {
            ShutdownClientRpc(countdownSecond);
        }
        
        bool willShutdown = countdownSecond <= 0;
        if (willShutdown)
        {
            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }
    
    [ClientRpc]
    private void ShutdownClientRpc(int countdownSecond)
    {
        if (IsHost)
        {
            return;
        }
        
        hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        if (countdownSecond <= 0)
        {
            //TODO clear/reset level
            //kick player using NetworkManager.Singleton.Shutdown();
            StartCoroutine(QuitToMainMenu());
        }
    }
    
    #endregion
    
    #region Scene Management
    
    private void OnActiveSceneChanged(Scene current, Scene next)
    {
#if UNITY_SERVER
        if (next.buildIndex == GameConstant.MenuSceneBuildIndex)
        {
            //TODO: ADD server shutdown
            // Debug.Log("server shutdown");
            NetworkManager.Singleton.Shutdown();
            DeregisterServer();
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
    }
    
    private void OnNetworkSceneEvent(SceneEvent sceneEvent)
    {
        bool isServer = sceneEvent.ClientId.Equals(NetworkManager.ServerClientId);
        bool isGameScene = !string.IsNullOrEmpty(sceneEvent.SceneName) && sceneEvent.SceneName.Equals(GameConstant.GameSceneName);

        BytewarsLogger.Log($"OnNetworkSceneEvent isServer:{isServer} type {sceneEvent.SceneEventType} " +
                           $"isGameScene:{isGameScene} clientId:{sceneEvent.ClientId}");
        
        if (!isServer)
        {
            return;
        }
        
        if (!isGameScene)
        {
            return;
        }
        
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            OnRejectBackfill?.Invoke();
            menuManager.CloseMenuPanel();
            Pool ??= new ObjectPooling(container, gamePrefabs, fxPrefabs);
            SetupGame();
        }
        
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && AllClientsSceneLoaded(sceneEvent))
        {
            SetInGameState(InGameState.PreGameCountdown);
        }
    }
    
    private bool AllClientsSceneLoaded(SceneEvent sceneEvent)
    {
        return connectedClients.Count == sceneEvent.ClientsThatCompleted.Count;
    }
    
    public static async Task ShowTravelingLoading()
    {
        MenuManager.Instance.ShowLoading("Traveling");
        await Task.Delay(TimeSpan.FromSeconds(TravelingDelay));
    }

    #endregion

    #region Network Management
    
    public static void StartListenNetworkSceneEvent()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= Instance.OnNetworkSceneEvent;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += Instance.OnNetworkSceneEvent;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && !IsServer)
        {
            ClientConnectedServerRpc(NetworkObjectId);
        }
        base.OnNetworkSpawn();
    }
    
    public void StartAsClient(string address, ushort port, InGameMode inGameMode)
    {
        var initialData = new InitialConnectionData
        {
            inGameMode = inGameMode, 
            sessionId = null
        };

        reconnect.ConnectAsClient(unityTransport, address, port, initialData);
    }
    
    public void StartAsHost(string address, ushort port, InGameMode inGameMode, string serverSessionId)
    {
        
        var initialData = new InitialConnectionData()
            { inGameMode = inGameMode, serverSessionId = serverSessionId };
        reconnect.StartAsHost(unityTransport, address, port, initialData);
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
    }
    
    /// <summary>
    /// called by server to notify client that the playerstate and teamstate for the player are updated
    /// </summary>
    /// <param name="teamStates"></param>
    /// <param name="playerStates"></param>
    /// <param name="inGameMode"></param>
    /// <param name="isInGameScene"></param>
    [ClientRpc]
    public void SendConnectedPlayerStateClientRpc(TeamState[] teamStates, PlayerState[] playerStates,
                                                  InGameMode inGameMode, ServerType serverType, bool isInGameScene)
    {
        //client side, because the previous playerState only exists in server, clientrpc is called on client
        BytewarsLogger.Log($"update player state lobby playerStates: {JsonUtility.ToJson(playerStates)} " +
                           $"teamStates: {JsonUtility.ToJson(teamStates)}");
        serverHelper.UpdatePlayerStates(teamStates, playerStates);
        InGameMode = inGameMode;
        GameData.GameModeSo = availableInGameMode[(int)inGameMode];
        GameData.ServerType = serverType;

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
        reconnect.ConnectAsClient(unityTransport, address, port, initialConnectionData);
    }

    #endregion
}