// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
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
    [SerializeField]
    private EventSystem _eventSystem;
    [SerializeField]
    private InGameHUD _hud;
    [SerializeField]
    private Camera _camera;
    [SerializeField]
    private GameEntityAbs[] _gamePrefabs;
    [SerializeField]
    private FxEntity[] _FxPrefabs;
    [SerializeField]
    private GameModeSO[] availableInGameMode;
    [SerializeField]
    private Reconnect reconnect;
    [SerializeField]
    private Transform _container;

    private static GameManager _instance;
    public static GameManager Instance => _instance;
    private GameModeEnum _gameMode = GameModeEnum.MainMenu;
    private MenuManager _menuManager;
    private Dictionary<string, GameEntityAbs> _gamePrefabDict = new Dictionary<string, GameEntityAbs>();

    public delegate void GameOverDelegate(GameModeEnum gameMode, InGameMode inGameMode, List<PlayerState> playerStates);
    public static event GameOverDelegate OnGameOver = delegate { };


    [SerializeField] private InGameCamera _inGameCamera;
    //private List<PlayerController> _instantiatedPlayerControllers = new List<PlayerController>();
    public InGameState InGameState => _inGameState;
    public List<GameEntityAbs> ActiveGEs { get; } = new List<GameEntityAbs>();
    public Dictionary<ulong, Player> Players { get; } = new Dictionary<ulong, Player>();
    public ObjectPooling Pool { get; private set; }
    public Camera MainCamera => _camera;
    public TeamState[] TeamStates => _serverHelper.GetTeamStates();
    public Dictionary<int, TeamState> ConnectedTeamStates => _serverHelper.ConnectedTeamStates;
    public Dictionary<ulong, PlayerState> ConnectedPlayerStates => _serverHelper.ConnectedPlayerStates;
    public ulong ClientNetworkId => _clientHelper.ClientNetworkId;
    private readonly Dictionary<int, Planet> _planets = new Dictionary<int, Planet>();
    private InGameState _inGameState = InGameState.None;
    private readonly ServerHelper _serverHelper = new ServerHelper();
    private readonly ClientHelper _clientHelper = new ClientHelper();
    private InGameMode _inGameMode = InGameMode.None;
    private Dictionary<ulong, GameClientController> connectedClients = new Dictionary<ulong, GameClientController>();
    private const int MinPlayerForOnlineGame = 2;
    private const int MinTeamForOnlineGame = 2;
    private DebugImplementation debug;
    private readonly ConnectionHelper connectionHelper = new ConnectionHelper();
    private List<Vector3> availablePositions;
    private const string ClassName = "[GameManager]";

    public void TriggerPauseLocalGame()
    {
        if (_inGameState == InGameState.LocalPause)
        {
            SetInGameState(InGameState.Playing);
            _menuManager.HideAnimate(null);
        }
        else if (_inGameState == InGameState.Playing)
        {
            _menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas);
            SetInGameState(InGameState.LocalPause);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }
    void Start()
    {
        foreach (var gamePrefab in _gamePrefabs)
        {
            _gamePrefabDict.Add(gamePrefab.name, gamePrefab);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        if (_unityTransport == null)
            _unityTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
#if UNITY_SERVER
        StartServer();
#endif
        if (debug == null)
            debug = new DebugImplementation();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        _hud.Reset();
        StartWait();
    }

    private void OnServerStopped(bool isHost)
    {
        BytewarsLogger.Log("Host server stop");
        connectedClients.Clear();
        _serverHelper.Reset();
    }

    private void OnClientStopped(bool isHost)
    {
        connectedClients.Clear();
        reconnect.OnClientStopped(isHost, _inGameState, _serverHelper,
            _clientHelper.ClientNetworkId, _inGameMode);
    }

    private void StartServer()
    {
        if (_unityTransport)
        {
            _unityTransport.ConnectionData.Address = ConnectionHandler.GetLocalIPAddress(); //"127.0.0.1"
            _unityTransport.ConnectionData.Port = ConnectionHandler.GetPort(); //7778
            _unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";

            NetworkManager.Singleton.StartServer();
            NetworkManager.SceneManager.OnSceneEvent += OnNetworkSceneEvent;

            Debug.Log("Server Address: " + _unityTransport.ConnectionData.ServerListenAddress.ToString());
            Debug.Log("Server Port: " + _unityTransport.ConnectionData.Port.ToString());
            Debug.Log("server started");
        }
    }

    private void OnNetworkSceneEvent(SceneEvent sceneEvent)
    {
        bool isServer = sceneEvent.ClientId == NetworkManager.ServerClientId;
        bool isGameScene = !String.IsNullOrEmpty(sceneEvent.SceneName) && sceneEvent.SceneName.Equals(GameConstant.GameSceneName);
        Debug.Log($"OnNetworkSceneEvent isServer:{isServer} type {sceneEvent.SceneEventType.ToString()} " +
                  $"isGameScene:{isGameScene} clientId:{sceneEvent.ClientId}");
        if (isServer && sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
            isGameScene)
        {
            OnRejectBackfill?.Invoke();
            MenuManager.Instance.CloseMenuPanel();
            if (Pool == null)
                Pool = new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);
            SetupGame();
        }
        if (isServer && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted &&
            isGameScene && AllClientsSceneLoaded(sceneEvent))
        {
            SetInGameState(InGameState.PreGameCountdown);
        }
    }

    private bool AllClientsSceneLoaded(SceneEvent sceneEvent)
    {
        return connectedClients.Count == sceneEvent.ClientsThatCompleted.Count;
    }

    private void OnClientDisconnected(ulong clientNetworkId)
    {
        string reason = "";
        int activeSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        bool isInMainMenu = activeSceneBuildIndex == GameConstant.MenuSceneBuildIndex;
        bool isInGameScene = activeSceneBuildIndex == GameConstant.GameSceneBuildIndex;
        if (!String.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            reason = NetworkManager.Singleton.DisconnectReason;
        }
        Debug.Log($"OnClientDisconnected client id {clientNetworkId} disconnected reason:{reason} active entity count:{ActiveGEs.Count} IsServer:{IsServer}");
        if (isInMainMenu)
        {
            OnDisconnectedInMainMenu?.Invoke(reason);
            if (_menuManager.IsLoading)
            {
                _menuManager.HideLoading();
            }
            else
            {
                if (IsServer)
                {
                    RemoveConnectedClient(clientNetworkId, isInGameScene);
                    var currentMenu = _menuManager.GetCurrentMenu();
                    if (currentMenu is MatchLobbyMenu lobby)
                    {
                        if (IsHost)
                        {
                            lobby.Refresh();
                        }
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
            }
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

    private void OnLobbyShutdownCountdown(int countdownSeconds)
    {
        //no player connected no need to update client UI for lobby shutdown
        Debug.Log($"{ClassName} OnLobbyShutdownCountdown countdown:{countdownSeconds}");
        if (countdownSeconds <= 0)
        {
            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }

    public void RemoveConnectedClient(ulong clientNetworkId, bool isInGameScene, bool isResetMissile = true)
    {
        connectedClients.Remove(clientNetworkId);
        if (Players.TryGetValue(clientNetworkId, out var player))
        {
            if (!IsServer)
            {
                player.gameObject.SetActive(false);
                Players.Remove(clientNetworkId);
            }
        }

        if (!isInGameScene || _inGameState == InGameState.GameOver)
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
            return;
        UpdateInGamePlayerState(teamStates, playerStates);
        connectedClients.Remove(clientNetworkId);
        if (Players.Remove(clientNetworkId, out var player))
        {
            if (isResetMissile)
            {
                player.Reset();
            }
            else
            {
                player.gameObject.SetActive(false);
            }
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
        var result = await connectionHelper.ConnectionApproval(request, response, IsServer, _inGameState, availableInGameMode,
            _inGameMode, _serverHelper);
        if (result != null)
        {
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
    }

    /// <summary>
    /// this is where the magic begin, most variable exist only on IsServer bracket, but not on IsClient
    /// </summary>
    /// <param name="clientNetworkId"></param>
    private void OnClientConnected(ulong clientNetworkId)
    {
        reconnect.OnClientConnected(clientNetworkId, IsOwner, IsServer, IsClient, IsHost, _serverHelper,
            _inGameMode, connectedClients, _inGameState, Players, _gameTimeLeft, _clientHelper);
    }

    private void ReAddReconnectedPlayerOnClient(ulong clientNetworkId, int[] firedMissilesId,
        TeamState[] teamStates, PlayerState[] playerStates)
    {
        Debug.Log($"ReAddReconnectedPlayerOnClient IsServer:{IsServer} clientNetworkId:{clientNetworkId}");
        _clientHelper.SetClientNetworkId(clientNetworkId);
        _hud.HideGameStatusContainer();
        var player = InGameFactory.SpawnReconnectedShip(clientNetworkId, _serverHelper, Pool);
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
            return;
        ReAddReconnectedPlayerOnClient(clientNetworkId, firedMissilesId, teamStates, playerStates);
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

    public override void OnNetworkSpawn()
    {
        if (IsOwner && !IsServer)
        {
            ClientConnectedServerRpc(NetworkObjectId);
        }
        base.OnNetworkSpawn();
    }

    async void StartWait()
    {
        await Task.Delay(150);
        while (!MenuManager.Instance.IsInitiated)
        {
            await Task.Delay(50);
        }
        _menuManager = MenuManager.Instance;
        _menuManager.SetEventSystem(_eventSystem);
        if (IsServer)
        {
            _menuManager.CloseMenuPanel();
            OnRegisterServer?.Invoke();
        }
    }

    private void OnActiveSceneChanged(Scene current, Scene next)
    {
        bool isInGameScene = next.buildIndex == GameConstant.GameSceneBuildIndex;
        bool isInMainMenuScene = next.buildIndex == GameConstant.MenuSceneBuildIndex;
#if UNITY_SERVER
        if (next.buildIndex==GameConstant.MenuSceneBuildIndex)
        {
            //TODO: ADD server shutdown
            // Debug.Log("server shutdown");
            NetworkManager.Singleton.Shutdown();
            DeregisterServer();
        }
#else
        if (NetworkManager.Singleton.IsListening)
            return;
        if (isInGameScene)
        {
            AudioManager.Instance.PlayGameplayBGM();
            MenuManager.Instance.CloseMenuPanel();
            SetupGame();
        }

        if (isInMainMenuScene)
        {
            Pool?.ResetAll();
        }
#endif
    }

    void SetupGame()
    {
        if (!SetInGameState(InGameState.Initializing))
            return;
        Pool ??= new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);
        Debug.Log(("Setup Game"));
        ActiveGEs.RemoveAll((GameEntityAbs ge) => !ge);
        Players.Clear();
        InGameStateResult states = new InGameStateResult();
        bool isOnlineGame = _gameMode == GameModeEnum.OnlineMultiplayer;
        if (isOnlineGame)
        {
            if (IsServer)
            {
                _hud.gameObject.SetActive(true);
                _hud.Init(_serverHelper.ConnectedTeamStates.Values.ToArray(), _serverHelper.ConnectedPlayerStates.Values.ToArray());
                var createLevelResult = InGameFactory.CreateLevel(GameData.GameModeSo, ActiveGEs,
                    Players, Pool, _serverHelper.ConnectedTeamStates, _serverHelper.ConnectedPlayerStates);
                availablePositions = createLevelResult.AvailablePositions;
                PlaceObjectsClientRpc(createLevelResult.LevelObjects, Players.Keys.ToArray(),
                        createLevelResult.AvailablePositions.ToArray(), _serverHelper.ConnectedTeamStates.Values.ToArray(),
                        _serverHelper.ConnectedPlayerStates.Values.ToArray());

            }
        }
        else
        {
            //create local offline teamStates and playerStates based on GameModeSO player per team count
            states = InGameFactory.CreateLocalGameState(GameData.GameModeSo);
            _serverHelper.SetTeamAndPlayerState(states);
            var result = InGameFactory.CreateLevel(GameData.GameModeSo, ActiveGEs,
                Players, Pool, states.m_teamStates, states.m_playerStates);
            availablePositions = result.AvailablePositions;
            if (_hud)
            {
                _hud.gameObject.SetActive(true);
                _hud.Init(states.m_teamStates.Values.ToArray(), states.m_playerStates.Values.ToArray());
            }

            if (_gameMode == GameModeEnum.SinglePlayer)
            {
                if (Players.TryGetValue(1, out var player))
                {
                    player.playerInput.enabled = false;
                }
            }
            SetInGameState(InGameState.PreGameCountdown);

        }

    }

    private void OnPreGameTimerUpdated(int timerSecond)
    {
        _hud.UpdatePreGameCountdown(timerSecond);
        if (timerSecond == 0)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count < MinPlayerForOnlineGame)
                {
                    SetInGameState(InGameState.ShuttingDown);
                }
                else
                {
                    SetInGameState(InGameState.Playing);
                }
            }
            else
            {
                SetInGameState(InGameState.Playing);
            }
        }
        if (NetworkManager.Singleton.IsListening)
            UpdatePreGameCountdownClientRpc(timerSecond);
    }

    [ClientRpc]
    private void UpdatePreGameCountdownClientRpc(int second)
    {
        if (IsHost)
            return;
        _hud.UpdatePreGameCountdown(second);
    }

    [ClientRpc]
    private void PlaceObjectsClientRpc(LevelObject[] levelObjects, ulong[] playersClientIds,
        Vector3[] availablePositionsP, TeamState[] teamStates, PlayerState[] playerStates)
    {
        Debug.Log($"PlaceObjectsClientRpc IsLocalPlayer:{IsLocalPlayer} IsClient:{IsClient} IsHost:{IsHost}");
        if (IsHost)
            return;
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        Pool ??= new ObjectPooling(_container, _gamePrefabs, _FxPrefabs);
        availablePositions = new List<Vector3>(availablePositionsP);
        AudioManager.Instance.PlayGameplayBGM();
        _clientHelper.PlaceObjectsOnClient(levelObjects, playersClientIds, Pool,
            _gamePrefabDict, _planets, Players, _serverHelper, ActiveGEs);
        _menuManager.CloseMenuPanel();
        _hud.gameObject.SetActive(true);
        _hud.Init(teamStates, playerStates);
    }

    private int _gameTimeLeft = 0;
    private void OnGameTimeUpdated(int remainingTime)
    {
        if (InGameState == InGameState.Playing)
        {
            _gameTimeLeft = remainingTime;
            _hud.SetTime(remainingTime);
            UpdateGameTimeClientRpc(remainingTime);
            if (remainingTime <= 0)
            {
                SetInGameState(InGameState.GameOver);
            }
        }
    }

    [ClientRpc]
    private void UpdateGameTimeClientRpc(int remainingTimeSecond)
    {
        if (IsHost)
            return;
        _hud.SetTime(remainingTimeSecond);
    }

    public void StartGame(GameModeSO gameModeSo)
    {
        //TODO change to switch to in game menu
        //hide current shown menu
        _menuManager.CloseMenuPanel();
        GameData.GameModeSo = gameModeSo;
        _gameMode = gameModeSo.gameMode;
        SceneManager.LoadScene(GameConstant.GameSceneBuildIndex);
    }

    public void RestartLocalGame()
    {
        _menuManager.HideAnimate(null);
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
        SetInGameState(InGameState.None);
        _menuManager.HideAnimate(null);
        OnClientLeaveSession?.Invoke();
        _menuManager.ChangeToMainMenu(GameConstant.MenuSceneBuildIndex);
    }

    public void OnObjectHit(Player player, Missile missile)
    {
        CollisionHelper.OnObjectHit(player, missile, Players, _serverHelper,
            _hud, this, _gameMode, availablePositions);
    }

    [ClientRpc]
    public void RepositionPlayerClientRpc(ulong clientNetworkId, Vector3 position,
        int maxInFlightMissile, Vector4 teamColor, Quaternion rotation)
    {
        if (IsHost)
            return;
        if (Players.TryGetValue(clientNetworkId, out var player))
        {
            player.Reset();
            player.transform.rotation = rotation;
            player.PlayerState.position = position;
            player.Init(maxInFlightMissile, teamColor);

        }
    }

    [ClientRpc]
    public void ResetPlayerClientRpc(ulong clientNetworkId)
    {
        if (IsHost)
            return;
        if (Players.TryGetValue(clientNetworkId, out var player))
        {
            player.Reset();
        }
    }

    [ClientRpc]
    public void UpdateScoreClientRpc(PlayerState playerState, PlayerState[] playerStates)
    {
        if (IsHost)
            return;
        _hud.UpdateKillsAndScore(playerState, playerStates);
    }

    [ClientRpc]
    public void UpdateLiveClientRpc(int teamIndex, int lives)
    {
        if (IsHost)
            return;
        _hud.SetLivesValue(teamIndex, lives);
    }

    public void CheckForGameOverCondition(bool isGameOver)
    {
        if (_gameMode is GameModeEnum.SinglePlayer or GameModeEnum.LocalMultiplayer)
        {
            if (isGameOver)
            {
                EndGame();
            }
        }
        else
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (isGameOver)
                {
                    UpdatePlayerStatesClientRpc(_serverHelper.ConnectedTeamStates.Values.ToArray(),
                        _serverHelper.ConnectedPlayerStates.Values.ToArray());
                    EndGame();
                }
            }
        }
    }

    [ClientRpc]
    public void UpdatePlayerStatesClientRpc(TeamState[] teamStates, PlayerState[] playerStates)
    {
        if (IsHost)
            return;
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        //TODO update game clients UI
    }

    private void EndGame()
    {
        SetInGameState(InGameState.GameOver);
    }

    public bool SetInGameState(InGameState state)
    {
        Debug.Log("try SetInGameState: " + state);
        if (_inGameState == state)
            return false;
        if (_hud)
            _hud.HideGameStatusContainer();
        int remainingGameDuration = GameData.GameModeSo.gameDuration;
        if (_gameTimeLeft > 0)
        {
            remainingGameDuration = _gameTimeLeft;
        }
        _inGameState = state;
        switch (state)
        {
            case InGameState.None:
                _hud.gameObject.SetActive(false);
                ResetLevel();
#if !UNITY_SERVER
                OnGameStateIsNone?.Invoke();
#endif
                break;
            case InGameState.Initializing:
                if (_hud)
                    _hud.gameObject.SetActive(false);
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
                _serverHelper.StartCoroutineCountdown(this, remainingGameDuration, OnGameTimeUpdated);
                break;
            case InGameState.ShuttingDown:
                _hud.gameObject.SetActive(true);
                _serverHelper.CancelCountdown();
                _serverHelper.StartCoroutineCountdown(this, GameData.GameModeSo.beforeShutDownCountdownSecond,
                    OnShutdownCountdownUpdate);
                break;
            case InGameState.LocalPause:
                _hud.gameObject.SetActive(false);
                break;
            case InGameState.GameOver:
                OnGameOver.Invoke(_gameMode, _inGameMode, ConnectedPlayerStates.Values.ToList());

                _serverHelper.CancelCountdown();
                if (NetworkManager.Singleton.IsListening)
                {
                    if (GameData.GameModeSo.gameOverShutdownCountdown > -1)
                    {
                        _serverHelper.StartCoroutineCountdown(this, GameData.GameModeSo.gameOverShutdownCountdown,
                            OnGameOverShutDownCountdown);
                    }
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
        if (!NetworkManager.Singleton.IsListening && _inGameCamera)
        {
            _inGameCamera.enabled = state == InGameState.Playing;
        }
        if (IsServer)
        {
            UpdateInGameStateClientRpc(_inGameState, remainingGameDuration);
        }
        return true;
    }

    private void OnGameOverShutDownCountdown(int countdownSeconds)
    {
        bool willShutdown = countdownSeconds <= 0;
        _menuManager.UpdateGameOverCountdown(countdownSeconds);
        GameOverCountdownClientRpc(countdownSeconds);
        if (willShutdown)
        {
            StartCoroutine(QuitToMainMenu());
        }
    }

    [ClientRpc]
    private void GameOverCountdownClientRpc(int seconds)
    {
        if (IsHost)
            return;
        _menuManager.UpdateGameOverCountdown(seconds);
    }

    private const string NotEnoughPlayer = "Not enough players, shutting down DS in: ";
    private void OnShutdownCountdownUpdate(int countdownSecond)
    {
        bool willShutdown = countdownSecond <= 0;
        Debug.Log(String.Format("shutdown in {0}", countdownSecond));
        _hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        //this will kick player too if countdown is 0
        if (IsServer)
        {
            ShutdownClientRpc(countdownSecond);
        }
        if (willShutdown)
        {
            StartCoroutine(reconnect.ShutdownServer(DeregisterServer));
        }
    }

    [ClientRpc]
    private void ShutdownClientRpc(int countdownSecond)
    {
        if (IsHost)
            return;
        _hud.UpdateShutdownCountdown(NotEnoughPlayer, countdownSecond);
        if (countdownSecond <= 0)
        {
            //TODO clear/reset level
            //kick player using NetworkManager.Singleton.Shutdown();
            StartCoroutine(QuitToMainMenu());
        }
    }

    [ClientRpc]
    private void UpdateInGameStateClientRpc(InGameState inGameState, int remainingGameTime)
    {
        if (IsHost)
            return;
        if (_hud)
        {
            _hud.HideGameStatusContainer();
        }
        _inGameState = inGameState;
        if (inGameState == InGameState.Initializing)
        {
            _hud.SetTime(remainingGameTime);
        }
        if (inGameState == InGameState.GameOver)
        {
            _hud.gameObject.SetActive(false);
            _menuManager.ShowInGameMenu(AssetEnum.GameOverMenuCanvas);
        }
        _inGameCamera.enabled = _inGameState == InGameState.Playing;
    }

    private void ResetLevel()
    {
        _gameTimeLeft = 0;
        // Debug.Log($"active game entity count:{_activeGEs.Count}");
        foreach (var ge in ActiveGEs)
        {
            ge.Reset();
        }
        ActiveGEs.Clear();
        Players.Clear();
        _planets.Clear();
        _hud.Reset();
    }

    [RuntimeInitializeOnLoadMethod]
    private static void SingletonInstanceChecker()
    {
        if (GameConstant.MenuSceneBuildIndex == SceneManager.GetActiveScene().buildIndex
            && _instance == null)
        {
            _instance = Instantiate(AssetManager.Singleton.GameManagerPrefab);
        }
    }

    public void OnPause()
    {
        //TODO if this is online game what happened when a connected player pause the game
        TriggerPauseLocalGame();
    }

    private UnityTransport _unityTransport;

    public void StartAsClient(string address, ushort port, InGameMode inGameMode)
    {
        var initialData = new InitialConnectionData()
        { inGameMode = inGameMode, sessionId = null };
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
    public void SendConnectedPlayerStateClientRpc(TeamState[] teamStates,
        PlayerState[] playerStates, InGameMode inGameMode, bool isInGameScene)
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

    private void UpdateInGamePlayerState(TeamState[] teamStates, PlayerState[] playerStates)
    {
        _serverHelper.UpdatePlayerStates(teamStates, playerStates);
        if (SceneManager.GetActiveScene().buildIndex == GameConstant.MenuSceneBuildIndex)
        {
            var lobby = (MatchLobbyMenu)_menuManager.ChangeToMenu(AssetEnum.MatchLobbyMenuCanvas);
            lobby.Refresh();
        }
    }

    [ServerRpc]
    private void ClientConnectedServerRpc(ulong networkObjectId)
    {
        Debug.Log($"ClientConnectedServerRpc IsServer{IsServer} networkObjectId:{networkObjectId}");
    }

    public void StartOnlineGame()
    {
        _serverHelper.CancelCountdown();
        _menuManager.CloseMenuPanel();
        _gameMode = GameData.GameModeSo.gameMode;
        SetGameModeClientRpc(_gameMode);
        if (IsServer && IsOwner)
        {
            var status = NetworkManager.SceneManager.LoadScene(GameConstant.GameSceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load {GameConstant.GameSceneName} " +
                                 $"with a {nameof(SceneEventProgressStatus)}: {status}");
            }
        }
    }

    [ClientRpc]
    private void SetGameModeClientRpc(GameModeEnum gameMode)
    {
        if (IsHost)
            return;
        _gameMode = gameMode;
    }

    public void OnDisable()
    {
        Pool?.DestroyAll();
        Pool?.ClearAll();
        Players?.Clear();
        Debug.Log("GameManager OnDisable");
        _serverHelper.CancelCountdown();
    }

    [ClientRpc]
    public void MissileHitClientRpc(ulong playerClientNetworkId,
        int missileId, int planetId, Vector3 missileExpPos, Quaternion missileExpRot)
    {
        if (IsHost)
            return;
        // Debug.Log($"MissileHitPlanetClientRpc clientNetorkId:{playerClientNetworkId} missileId:{missileId} planetId:{planetId}");
        if (Players.TryGetValue(playerClientNetworkId, out var player))
        {
            player.ExplodeMissile(missileId, missileExpPos, missileExpRot);
        }

        if (planetId > -1 && _planets.TryGetValue(planetId, out var planet))
        {
            planet.OnHitByMissile();
        }
    }

    [ClientRpc]
    public void MissileSyncClientRpc(ulong playerClientNetworkId, int missileID, Vector3 velocity,
        Vector3 position, Quaternion rotation)
    {
        if (IsHost)
            return;
        if (Players.TryGetValue(playerClientNetworkId, out var player))
        {
            player.SyncMissile(missileID, velocity, position, rotation);
        }
    }
    public event Action OnClientLeaveSession;
    public event Action OnDeregisterServer;
    public event Action OnRegisterServer;
    public event Action OnRejectBackfill;
    public event Action OnGameStateIsNone;

    private async void DeregisterServer()
    {
#if UNITY_SERVER
        OnDeregisterServer?.Invoke();
#endif
        await Task.Delay(150);
        Debug.Log("GameManager Application.Quit");
    }

    public void StartAsClient(string address, ushort port, InitialConnectionData initialConnectionData)
    {
        reconnect.ConnectAsClient(_unityTransport, address, port, initialConnectionData);
    }

    /// <summary>
    /// called when client is disconnected and will send disconnect reason if available
    /// </summary>
    public static event Action<string> OnDisconnectedInMainMenu;

    /// <summary>
    /// P2P Host is server (but only for peer to peer) and also client
    /// this function should be called only on peer to peer host
    /// </summary>
    public static void StartListenNetworkSceneEvent()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= _instance.OnNetworkSceneEvent;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += _instance.OnNetworkSceneEvent;
    }
}