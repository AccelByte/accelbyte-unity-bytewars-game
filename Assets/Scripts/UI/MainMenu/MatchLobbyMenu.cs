using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MatchLobbyMenu : MenuCanvas
{
    [SerializeField] private PlayerEntry[] _playersEntries;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject statusContainer;
    
    private const string CountDownPrefix = "MATCH START IN: ";

    #region Initialization and Lifecycle

    private void OnEnable()
    {
        Refresh();
    }
    
    private void Awake()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }
    
    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    #endregion Initialization and Lifecycle
    
    #region UI Handling
    
    private void OnGameStateChanged(InGameState gameState)
    {
#if !UNITY_SERVER
        if (gameState is InGameState.None)
        {
            ResetStartButton();
        }
#endif
    }
    
    private void ResetPlayerEntries()
    {
        _playersEntries.ToList().ForEach(entry => entry.gameObject.SetActive(false));
    }

    private static void OnStartButtonClicked()
    {
        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (!playerObj)
        {
            return;
        }

        var gameController = playerObj.GetComponent<GameClientController>();
        if (gameController)
        {
            gameController.StartOnlineGame();
        }
    }

    private void OnQuitButtonClicked()
    {
        StartCoroutine(LeaveSessionAndQuit());
    }
    
    private void ResetStartButton()
    {
        startButton.gameObject.SetActive(true);
    }
    
    public void ShowStatus(string status)
    {
        if (!statusContainer.activeSelf)
        {
            statusContainer.SetActive(true);
        }
        
        statusLabel.text = status;
    }
    
    #endregion UI Handling
    
    #region Override Functions
    
    public override GameObject GetFirstButton() => startButton.gameObject;
    
    public override AssetEnum GetAssetEnum() => AssetEnum.MatchLobbyMenuCanvas;
    
    #endregion Override Functions

    public void Refresh()
    {
        ResetPlayerEntries();
        
        PopulatePlayerEntries();

        /* If P2P, only show the start button if the player is the session leader.
         * Otherwise, always show the start button on other server mode. */
        startButton.gameObject.SetActive(
            GameData.ServerType.Equals(ServerType.OnlinePeer2Peer) ?
            GameManager.Instance.IsHost :
            true);
    }
    
    private void PopulatePlayerEntries()
    {
        ulong clientNetworkId = GameManager.Instance.ClientNetworkId;
        Dictionary<ulong, PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates;
        Dictionary<int, TeamState> teamStates = GameManager.Instance.ConnectedTeamStates;
        
        foreach (KeyValuePair<ulong, PlayerState> kvp in playerStates)
        {
            PlayerState playerState = kvp.Value;
            bool isCurrentPlayer = playerState.clientNetworkId == clientNetworkId;
            
            SpawnPlayer(teamStates[playerState.teamIndex], playerState, isCurrentPlayer);
        }
    }
    
    private void SpawnPlayer(TeamState teamState, PlayerState playerState, bool isCurrentPlayer)
    {
        foreach (var playerEntry in _playersEntries)
        {
            if (playerEntry.gameObject.activeSelf)
            {
                continue;
            }
            
            playerEntry.Set(teamState, playerState, isCurrentPlayer);
            playerEntry.gameObject.SetActive(true);
            break;
        }
    }
    
    public void Countdown(int second)
    {
        ShowStatus(CountDownPrefix + second);
    }

    private static IEnumerator LeaveSessionAndQuit()
    {
        //TODO intentionally quit from lobby, server should shutdown when there is no player and the lobby is custom match
        yield return GameManager.Instance.QuitToMainMenu();
    }
}
