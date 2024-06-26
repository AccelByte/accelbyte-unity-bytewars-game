﻿// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
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
    [SerializeField] private EventSystem _eventSystem;
    [SerializeField] private InGameHUD _hud;
    [SerializeField] private Camera _camera;
    [SerializeField] private GameEntityAbs[] _gamePrefabs;
    [SerializeField] private FxEntity[] _FxPrefabs;
    [SerializeField] private GameModeSO[] availableInGameMode;
    [SerializeField] private Reconnect reconnect;
    [SerializeField] private Transform _container;
    [SerializeField] private InGameCamera _inGameCamera;
    
    public static GameManager Instance { get; private set; }
    
    public delegate void GameOverDelegate(GameModeEnum gameMode, InGameMode inGameMode, List<PlayerState> playerStates);
    public static event GameOverDelegate OnGameOver = delegate {};
    public static event Action<string> OnDisconnectedInMainMenu;
    public static event Action<InGameState> OnGameStateChanged;
    
    public event Action OnClientLeaveSession;
    public event Action OnDeregisterServer;
    public event Action OnRegisterServer;
    public event Action OnRejectBackfill;
    
    public InGameState InGameState { get; private set; } = InGameState.None;
    public InGamePause InGamePause { get; private set; }
    public List<GameEntityAbs> ActiveGEs { get; } = new();
    public Dictionary<ulong, Player> Players { get; } = new();
    public ObjectPooling Pool { get; private set; }
    public Camera MainCamera => _camera;
    public TeamState[] TeamStates => _serverHelper.GetTeamStates();
    public Dictionary<int, TeamState> ConnectedTeamStates => _serverHelper.ConnectedTeamStates;
    public Dictionary<ulong, PlayerState> ConnectedPlayerStates => _serverHelper.ConnectedPlayerStates;
    public ulong ClientNetworkId => _clientHelper.ClientNetworkId;
    
    private const string NotEnoughPlayer = "Not enough players, shutting down DS in: ";
    private const int MinPlayerForOnlineGame = 2;
    private const int MinTeamForOnlineGame = 2;
    
    private readonly Dictionary<ulong, GameClientController> connectedClients = new();
    private readonly Dictionary<string, GameEntityAbs> _gamePrefabDict = new();
    private readonly Dictionary<int, Planet> _planets = new();
    private readonly ConnectionHelper connectionHelper = new();
    private readonly ServerHelper _serverHelper = new();
    private readonly ClientHelper _clientHelper = new();
    
    private InGameMode _inGameMode = InGameMode.None;
    private GameModeEnum _gameMode = GameModeEnum.MainMenu;
    private List<Vector3> availablePositions;
    private UnityTransport _unityTransport;
    private DebugImplementation debug;
    private MenuManager _menuManager;
    private int _gameTimeLeft;

    #region Initialization and Lifecycle

    [RuntimeInitializeOnLoadMethod]
    private static void CreateInstance()
    {
        bool isMainMenuScene = SceneManager.GetActiveScene().buildIndex is GameConstant.MenuSceneBuildIndex;
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
        foreach (GameEntityAbs gamePrefab in _gamePrefabs)
        {
            _gamePrefabDict.Add(gamePrefab.name, gamePrefab);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        if (_unityTransport == null)
        {
            _unityTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        }

#if UNITY_SERVER
        StartServer();
#endif

        debug ??= new DebugImplementation();
        _hud.Reset();
        
        InitMenuManagerWhenReady().ContinueWith(_ =>
        {
            if (IsServer)
            {
                _menuManager.CloseMenuPanel();
                OnRegisterServer?.Invoke();
            }
            
            InGamePause = new InGamePause(_menuManager, _hud, this);
        });
    }
    
    public void OnDisable()
    {
        Pool?.DestroyAll();
        Pool?.ClearAll();
        Players?.Clear();
        BytewarsLogger.Log("GameManager OnDisable");
        _serverHelper.CancelCountdown();
    }
    
    private async Task InitMenuManagerWhenReady()
    {
        while (!MenuManager.Instance.IsInitiated)
        {
            await Task.Delay(50);
        }
        
        _menuManager = MenuManager.Instance;
        _menuManager.SetEventSystem(_eventSystem);
    }

    private void SetupGame()
    {
        if (!SetInGameState(InGameState.Initializing))
        {
            return;
        }

        BytewarsLogger.Log("Setup Game");

        Pool ??= new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);

        ActiveGEs.RemoveAll(ge => !ge);
        Players.Clear();
        
        if (_gameMode is GameModeEnum.OnlineMultiplayer)
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
        
        _serverHelper.SetTeamAndPlayerState(states);
        
        CreateLevelAndInitializeHud(states.m_teamStates, states.m_playerStates, out _);
        
        bool isSinglePlayer = _gameMode is GameModeEnum.SinglePlayer;
        if (isSinglePlayer && Players.TryGetValue(1, out Player player))
        {
            player.playerInput.enabled = false;
        }
        
        SetInGameState(InGameState.PreGameCountdown);
    }
    
    private void SetupOnlineGame()
    {
        if (!IsServer)
        {
            return;
        }
        
        if (_hud != null)
        {
            _hud.gameObject.SetActive(true);
            _hud.Init(_serverHelper.ConnectedTeamStates.Values.ToArray(),
                      _serverHelper.ConnectedPlayerStates.Values.ToArray());
        }
        
        CreateLevelAndInitializeHud(_serverHelper.ConnectedTeamStates,
                                    _serverHelper.ConnectedPlayerStates,
                                    out CreateLevelResult result);
        
        PlaceObjectsClientRpc(result.LevelObjects, Players.Keys.ToArray(),
                              result.AvailablePositions.ToArray(),
                              _serverHelper.ConnectedTeamStates.Values.ToArray(),
                              _serverHelper.ConnectedPlayerStates.Values.ToArray());
    }
    
    private void CreateLevelAndInitializeHud(Dictionary<int, TeamState> teamStates,
                                             Dictionary<ulong, PlayerState> playerStates,
                                             out CreateLevelResult result)
    {
        result = InGameFactory.CreateLevel(GameData.GameModeSo, ActiveGEs, Players, Pool, teamStates, playerStates);
        availablePositions = result.AvailablePositions;
        
        if (_hud == null)
        {
            return;
        }
        
        _hud.gameObject.SetActive(true);
        _hud.Init(teamStates.Values.ToArray(), playerStates.Values.ToArray());
    }

    #endregion

    #region Connection Management

    private void OnServerStopped(bool isHostStopped)
    {
        if (isHostStopped)
        {
            BytewarsLogger.Log("Hosting server has stopped");
            _inGameMode = InGameMode.None;
        }

        connectedClients.Clear();
        _serverHelper.Reset();
    }


    private void OnClientStopped(bool isHostStopped)
    {
        if (isHostStopped)
        {
            connectedClients.Clear();
        }

        reconnect.OnClientStopped(isHostStopped, InGameState, _serverHelper,
            _clientHelper.ClientNetworkId, _inGameMode);
    }


    private void StartServer()
    {
        if (!_unityTransport)
        {
            return;
        }

        _unityTransport.ConnectionData.Address = ConnectionHandler.GetLocalIPAddress();
        _unityTransport.ConnectionData.Port = ConnectionHandler.GetPort();
        _unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";

        NetworkManager.Singleton.StartServer();
        NetworkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;

        BytewarsLogger.Log("Server Address: " + _unityTransport.ConnectionData.ServerListenAddress.ToString());
        BytewarsLogger.Log("Server Port: " + _unityTransport.ConnectionData.Port.ToString());
        BytewarsLogger.Log("server started");
    }
    
    /// <summary>
    /// this is where the magic begin, most variable exist only on IsServer bracket, but not on IsClient
    /// </summary>
    /// <param name="clientNetworkId"></param>
    private void OnClientConnected(ulong clientNetworkId)
    {
        reconnect.OnClientConnected(clientNetworkId, IsOwner, IsServer, IsClient, IsHost, _serverHelper,
                                    _inGameMode, connectedClients, InGameState, Players, _gameTimeLeft, _clientHelper);
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        string reason = string.Empty;
        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        bool isInMainMenu = activeSceneBuildIndex is GameConstant.MenuSceneBuildIndex;
        bool isInGameScene = activeSceneBuildIndex is GameConstant.GameSceneBuildIndex;

        if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            reason = NetworkManager.Singleton.DisconnectReason;
        }

        BytewarsLogger.Log($"OnClientDisconnected client id {clientNetworkId} disconnected reason:{reason} " +
                  $"active entity count:{ActiveGEs.Count} IsServer:{IsServer}");

        if (isInMainMenu)
        {
            OnDisconnectedInMainMenu?.Invoke(reason);

            if (_menuManager.IsLoading)
            {
                _menuManager.HideLoading();
            }

            if (!IsServer)
            {
                return;
            }

            RemoveConnectedClient(clientNetworkId, isInGameScene);
            MenuCanvas currentMenu = _menuManager.GetCurrentMenu();

            if (IsHost && currentMenu is MatchLobbyMenu lobby)
            {
                lobby.Refresh();
            }

            if (reconnect.IsServerShutdownOnLobby(connectedClients.Count))
            {
                _serverHelper.StartCoroutineCountdown(this, 
                    GameData.GameModeSo.lobbyShutdownCountdown, OnLobbyShutdownCountdown);  
            }

            //wait reconnect
            // if (connectedClients.Count < 1)
            // {
            //     _serverHelper.CancelCountdown();
            //     _inGameMode = InGameMode.None;
            // }
        } 
        else if (isInGameScene)
        {
            if (IsServer)
            {
                //player might reconnect in the middle of game, missile will not reset
                RemoveConnectedClient(clientNetworkId, isInGameScene, false);

                if (connectedClients.Count < MinPlayerForOnlineGame)
                {
                    SetInGameState(InGameState.ShuttingDown);
                }
            }
        }

        if (IsClient && !IsHost)
        {
            StartCoroutine(QuitToMainMenu());
        }
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
                                              InGameState, availableInGameMode, _inGameMode, _serverHelper);
        if (result == null)
        {
            return;
        }
        
        if (_inGameMode == InGameMode.None)
        {
            _inGameMode = result.InGameMode;
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
            _serverHelper.RemovePlayerState(clientNetworkId);
        }
        else
        {
            _serverHelper.DisconnectPlayerState(clientNetworkId, player);
        }

        RemoveConnectedClientRpc(clientNetworkId, _serverHelper.ConnectedTeamStates.Values.ToArray(), 
            _serverHelper.ConnectedPlayerStates.Values.ToArray(), isResetMissile);
    }

    [ClientRpc]
    private void RemoveConnectedClientRpc(ulong clientNetworkId, TeamState[] teamStates,
        PlayerState[] playerStates, bool isResetMissile)
    {
        if (IsHost)
        {
            return;
        }

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

        _clientHelper.SetClientNetworkId(clientNetworkId);
        _hud.HideGameStatusContainer();

        Player player = InGameFactory.SpawnReconnectedShip(clientNetworkId, _serverHelper, Pool);
        if (player)
        {
            player.SetFiredMissilesID(firedMissilesId);
            Players.TryAdd(clientNetworkId, player);
        }

        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
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
        GameData.GameModeSo = gameModeSo;
        _gameMode = gameModeSo.gameMode;

        _menuManager.CloseMenuPanel();
        _menuManager.ShowLoading("Starting Game...");

        AudioManager.Instance.PlaySfx("Enter_Simulate");

        const float changeSceneDuration = 1.5f;
        CameraMovement.CancelMoveCameraLerp(_camera);
        Vector3 powerBarVisiblePosition = _menuManager.TargetCameraPositions.First();
        CameraMovement.MoveCameraLerp(_camera, powerBarVisiblePosition, changeSceneDuration);
        
        await Task.Delay(TimeSpan.FromSeconds(changeSceneDuration));
        _menuManager.HideLoading();

        SceneManager.LoadScene(GameConstant.GameSceneBuildIndex);
    }

    public void RestartLocalGame()
    {
        _menuManager.CloseInGameMenu();
        ResetLevel();
        SetupGame();
    }

    public IEnumerator QuitToMainMenu()
    {
        if (IsServer)
        {
            _serverHelper.CancelCountdown();
        }

        yield return reconnect.ClientDisconnectIntentionally();

        if (InGameState is InGameState.LocalPause)
        {
            Time.timeScale = 1f;
        }

        OnClientLeaveSession?.Invoke();
        
        _menuManager.CloseInGameMenu();
        _menuManager.ChangeToMainMenu();
    }

    public void OnObjectHit(Player player, Missile missile)
    {
        CollisionHelper.OnObjectHit(player, missile, Players, _serverHelper,
            _hud, this, _gameMode, availablePositions);
    }

    public void CheckForGameOverCondition(bool isGameOver)
    {
        bool isOfflineGame = _gameMode is GameModeEnum.SinglePlayer or GameModeEnum.LocalMultiplayer;

        if (isOfflineGame && isGameOver)
        {
            EndGame();
        }
        else if (NetworkManager.Singleton.IsServer && isGameOver)
        {
            UpdatePlayerStatesClientRpc(
                _serverHelper.ConnectedTeamStates.Values.ToArray(),
                _serverHelper.ConnectedPlayerStates.Values.ToArray());

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

        if (_hud)
        {
            _hud.HideGameStatusContainer();
        }

        int remainingGameDuration = GameData.GameModeSo.gameDuration;
        if (_gameTimeLeft > 0)
        {
            remainingGameDuration = _gameTimeLeft;
        }

        InGameState = state;
        switch (state)
        {
            case InGameState.None:
                _hud.gameObject.SetActive(false);
                ResetLevel();
                break;

            case InGameState.Initializing:
                if (_hud)
                {
                    _hud.gameObject.SetActive(false);
                }
                break;

            case InGameState.PreGameCountdown:
                if (_hud)
                {
                    _hud.gameObject.SetActive(true);
                    _hud.SetTime(remainingGameDuration);
                }

                _serverHelper.CancelCountdown();
                
                if (this)
                {
                    _serverHelper.StartCoroutineCountdown(this, 
                        GameData.GameModeSo.beforeGameCountdownSecond, 
                        OnPreGameTimerUpdated);
                }
                break;

            case InGameState.Playing:
                _hud.gameObject.SetActive(true);
                _serverHelper.CancelCountdown();
                _serverHelper.StartCoroutineCountdown(this, 
                    remainingGameDuration,
                    OnGameTimeUpdated);
                break;

            case InGameState.ShuttingDown:
                _hud.gameObject.SetActive(true);
                _serverHelper.CancelCountdown();
                _serverHelper.StartCoroutineCountdown(this, 
                    GameData.GameModeSo.beforeShutDownCountdownSecond, 
                    OnShutdownCountdownUpdate);
                break;

            case InGameState.LocalPause:
                _hud.gameObject.SetActive(false);
                break;

            case InGameState.GameOver:
                OnGameOver.Invoke(_gameMode, _inGameMode, ConnectedPlayerStates.Values.ToList());

                _serverHelper.CancelCountdown();

                bool isShuttingDown = GameData.GameModeSo.gameOverShutdownCountdown > -1;
                if (!IsLocalGame() && isShuttingDown)
                {
                    _serverHelper.StartCoroutineCountdown(this, 
                        GameData.GameModeSo.gameOverShutdownCountdown,
                        OnGameOverShutDownCountdown);
                }

                if (InGamePause.IsPausing())
                {
                    InGamePause.ToggleGamePause();
                }

                _hud.gameObject.SetActive(false);
                _menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);

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

        if (IsLocalGame() && _inGameCamera)
        {
            _inGameCamera.enabled = state == InGameState.Playing;
        }

        if (IsServer)
        {
            UpdateInGameStateClientRpc(InGameState, remainingGameDuration);
        }

        OnGameStateChanged?.Invoke(state);

        return true;
    }
    
    [ClientRpc]
    private void UpdateInGameStateClientRpc(InGameState inGameState, int remainingGameTime)
    {
        if (IsHost)
        {
            return;
        }
        
        if (_hud)
        {
            _hud.HideGameStatusContainer();
        }
        
        InGameState = inGameState;
        if (inGameState == InGameState.Initializing)
        {
            _hud.SetTime(remainingGameTime);
        }
        
        if (inGameState == InGameState.GameOver)
        {
            if (InGamePause.IsPausing())
            {
                InGamePause.ToggleGamePause();
            }
            
            _hud.gameObject.SetActive(false);
            _menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);
        }
        
        _inGameCamera.enabled = InGameState == InGameState.Playing;
    }

    private void ResetLevel()
    {
        _gameTimeLeft = 0;

        foreach (GameEntityAbs ge in ActiveGEs)
        {
            ge.Reset();
        }

        ActiveGEs.Clear();
        Players.Clear();
        _planets.Clear();
        _hud.Reset();
    }
    
    public async void StartOnlineGame()
    {
        _serverHelper.CancelCountdown();
        _gameMode = GameData.GameModeSo.gameMode;
        SetGameModeClientRpc(_gameMode);
        
        _menuManager.CloseMenuPanel();
        _menuManager.ShowLoading("Starting Game...");
        
        AudioManager.Instance.PlaySfx("Enter_Simulate");
        
        const float changeSceneDuration = 1.5f;
        CameraMovement.CancelMoveCameraLerp(_camera);
        Vector3 powerBarVisiblePosition = _menuManager.TargetCameraPositions.First();
        CameraMovement.MoveCameraLerp(_camera, powerBarVisiblePosition, changeSceneDuration);
        
        await Task.Delay(TimeSpan.FromSeconds(changeSceneDuration));
        _menuManager.HideLoading();
        
        if (!IsServer || !IsOwner)
        {
            return;
        }
        
        SceneEventProgressStatus status = NetworkManager.SceneManager.LoadScene(GameConstant.GameSceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
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
        
        _gameMode = gameMode;
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
        
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        Pool ??= new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);
        availablePositions = new List<Vector3>(availablePositionsP);
        
        _clientHelper.PlaceObjectsOnClient(levelObjects, playersClientIds, Pool,
                                           _gamePrefabDict, _planets, Players, _serverHelper, ActiveGEs);
        
        _menuManager.CloseMenuPanel();
        _hud.gameObject.SetActive(true);
        _hud.Init(teamStates, playerStates);
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
        
        _hud.UpdateKillsAndScore(playerState, playerStates);
    }
    
    [ClientRpc]
    public void UpdateLiveClientRpc(int teamIndex, int lives)
    {
        if (IsHost)
        {
            return;
        }
        
        _hud.SetLivesValue(teamIndex, lives);
    }
    
    private void UpdateInGamePlayerState(TeamState[] teamStates, PlayerState[] playerStates)
    {
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        if (SceneManager.GetActiveScene().buildIndex == GameConstant.MenuSceneBuildIndex)
        {
            var lobby = (MatchLobbyMenu)_menuManager.ChangeToMenu(AssetEnum.MatchLobbyMenuCanvas);
            lobby.Refresh();
        }
    }
    
    [ClientRpc]
    public void UpdatePlayerStatesClientRpc(TeamState[] teamStates, PlayerState[] playerStates)
    {
        if (IsHost)
        {
            return;
        }
        
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
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
        
        if (planetId > -1 && _planets.TryGetValue(planetId, out Planet planet))
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
        _menuManager.UpdateLobbyCountdown(countdown);
    }
    
    private void OnPreGameTimerUpdated(int timerSecond)
    {
        _hud.UpdatePreGameCountdown(timerSecond);
        bool areEnoughPlayersConnected =
            !IsLocalGame() && NetworkManager.Singleton.ConnectedClients.Count >= MinPlayerForOnlineGame;
        
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
        
        _hud.UpdatePreGameCountdown(second);
    }

    private void OnGameTimeUpdated(int remainingTime)
    {
        if (InGameState != InGameState.Playing)
        {
            return;
        }
        
        _gameTimeLeft = remainingTime;
        _hud.SetTime(remainingTime);
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
        
        _hud.SetTime(remainingTimeSecond);
    }
    
    private void OnGameOverShutDownCountdown(int countdownSeconds)
    {
        _menuManager.UpdateGameOverCountdown(countdownSeconds);
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
        
        _menuManager.UpdateGameOverCountdown(seconds);
    }
    
    private void OnShutdownCountdownUpdate(int countdownSecond)
    {
        BytewarsLogger.Log("shutdown in " + countdownSecond);
        _hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        
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
        
        _hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
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
        if (next.buildIndex is GameConstant.MenuSceneBuildIndex)
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
                _menuManager.CloseMenuPanel();
                SetupGame();
                break;

            case GameConstant.MenuSceneBuildIndex:
                SetInGameState(InGameState.None);
                _hud.gameObject.SetActive(false);
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
        
        if (sceneEvent.SceneEventType is SceneEventType.LoadComplete)
        {
            OnRejectBackfill?.Invoke();
            _menuManager.CloseMenuPanel();
            Pool ??= new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);
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

        reconnect.ConnectAsClient(_unityTransport, address, port, initialData);
    }
    
    public void StartAsHost(string address, ushort port, InGameMode inGameMode, string serverSessionId)
    {
        
        var initialData = new InitialConnectionData()
            { inGameMode = inGameMode, serverSessionId = serverSessionId };
        reconnect.StartAsHost(_unityTransport, address, port, initialData);
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
                                                  InGameMode inGameMode, bool isInGameScene)
    {
        //client side, because the previous playerState only exists in server, clientrpc is called on client
        BytewarsLogger.Log($"update player state lobby playerStates: {JsonUtility.ToJson(playerStates)} " +
                           $"teamStates: {JsonUtility.ToJson(teamStates)}");
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        _inGameMode = inGameMode;
        GameData.GameModeSo = availableInGameMode[(int)inGameMode];
        
        if (!isInGameScene)
        {
            var lobby = (MatchLobbyMenu)_menuManager.ChangeToMenu(AssetEnum.MatchLobbyMenuCanvas);
            lobby.Refresh();
            _menuManager.HideLoading(false);
        }
    }
    
    [ServerRpc]
    private void ClientConnectedServerRpc(ulong networkObjectId)
    {
        BytewarsLogger.Log($"ClientConnectedServerRpc IsServer{IsServer} networkObjectId:{networkObjectId}");
    }
    
    public void StartAsClient(string address, ushort port, InitialConnectionData initialConnectionData)
    {
        reconnect.ConnectAsClient(_unityTransport, address, port, initialConnectionData);
    }

    #endregion
}