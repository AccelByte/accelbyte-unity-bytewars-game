// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MatchLobbyMenu : MenuCanvas
{
    [SerializeField] private RectTransform teamEntryListPanel;
    [SerializeField] private TeamEntry teamEntryPrefab;
    [SerializeField] private PlayerEntry playerEntryPrefab;

    [SerializeField] private Button quitButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject statusContainer;

    private const string CountDownPrefix = "MATCH START IN: ";

    #region Initialization and Lifecycle

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        statusContainer.SetActive(false);
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
        GenerateTeamEntries();

        BytewarsLogger.Log($"Refresh match lobby. " +
            $"Is match session: {SessionCache.IsCreateMatch()}, Is session leader: {SessionCache.IsSessionLeader()}, " +
            $"Server type: {GameData.ServerType}, Is host: {NetworkManager.Singleton.IsHost}");
        
        if (SessionCache.IsCreateMatch())
        {
            startButton.gameObject.SetActive(SessionCache.IsSessionLeader());
        } 
        else
        {
            /* If P2P, only show the start button if the player is the session leader.
            * Otherwise, always show the start button on other server mode. */
            startButton.gameObject.SetActive(
                GameData.ServerType.Equals(ServerType.OnlinePeer2Peer) ?
                NetworkManager.Singleton.IsHost :
                true);
        }
    }
    
    private void GenerateTeamEntries()
    {
        ulong clientNetworkId = GameManager.Instance.ClientNetworkId;
        Dictionary<ulong, PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates;
        Dictionary<int, TeamState> teamStates = GameManager.Instance.ConnectedTeamStates;

        teamEntryListPanel.DestroyAllChildren();

        // Generate team and its member entries.
        Dictionary<int, TeamEntry> teamEntries = new Dictionary<int, TeamEntry>();
        foreach (KeyValuePair<ulong, PlayerState> kvp in playerStates.OrderBy(p => p.Value.TeamIndex))
        {
            PlayerState playerState = kvp.Value;
            int playerTeamIndex = playerState.TeamIndex;
            bool isCurrentPlayer = playerState.ClientNetworkId == clientNetworkId;

            if (!teamStates.ContainsKey(playerTeamIndex))
            {
                BytewarsLogger.Log($"Cannot spawn player entry. Invalid team index: {playerTeamIndex}");
                continue;
            }

            // Create the team entry if not yet.
            if (!teamEntries.ContainsKey(playerTeamIndex))
            {
                TeamEntry teamEntry = Instantiate(teamEntryPrefab, Vector3.zero, Quaternion.identity, teamEntryListPanel);
                teamEntry.Set(teamStates[playerTeamIndex]);
                teamEntries.Add(playerTeamIndex, teamEntry);
            }

            RectTransform teamEntryContainer = (RectTransform)teamEntries[playerTeamIndex].transform;
            RectTransform playerEntryContainer = teamEntries[playerTeamIndex].PlayerEntryContainer;

            // Create the player entry.
            PlayerEntry playerEntry = Instantiate(
                playerEntryPrefab, 
                Vector3.zero, 
                Quaternion.identity,
                playerEntryContainer);

            playerEntry.Set(teamStates[playerTeamIndex], playerState, isCurrentPlayer);

            LayoutRebuilder.ForceRebuildLayoutImmediate(playerEntryContainer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(teamEntryContainer);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(teamEntryListPanel);
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
