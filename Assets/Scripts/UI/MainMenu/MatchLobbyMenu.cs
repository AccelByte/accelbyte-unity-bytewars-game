using System;
using System.Collections;
using System.Collections.Generic;
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
    private ulong _clientNetworkId;
    private Dictionary<int, TeamState> _teamStates;
    private Dictionary<ulong, PlayerState> _playerStates;
    [SerializeField]
    private TextMeshProUGUI statusLabel;
    [SerializeField]
    private GameObject statusContainer;

    private void Start()
    {
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(ClickQuitBtn);
        GameManager.Instance.OnGameStateIsNone += ResetStartButton;

    }

    private void ResetStartButton()
    {
        startButton.gameObject.SetActive(true);
    }

    private void ClickQuitBtn()
    {
        StartCoroutine(LeaveSessionAndQuit());
    }
    private IEnumerator LeaveSessionAndQuit()
    {
        //TODO intentionally quit from lobby, server should shutdown when there is no player and the lobby is custom match
        yield return GameManager.Instance.QuitToMainMenu();
    }

    private void StartGame()
    {
        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObj)
        {
            var gameController = playerObj.GetComponent<GameClientController>();
            if (gameController)
            {
                gameController.StartOnlineGame();
            }
        }
    }

    private void Init()
    {
        foreach (var playerEntry in _playersEntries)
        {
            playerEntry.gameObject.SetActive(false);
        }
    }
    public void SpawnPlayer(TeamState teamState, PlayerState playerState, bool isCurrentPlayer)
    {
        foreach (var playerEntry in _playersEntries)
        {
            if (!playerEntry.gameObject.activeSelf)
            {
                playerEntry.Set(teamState, playerState, isCurrentPlayer);
                playerEntry.gameObject.SetActive(true);
                break;
            }
        }
    }

    public override GameObject GetFirstButton()
    {
        return startButton.gameObject;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        _clientNetworkId = GameManager.Instance.ClientNetworkId;
        _playerStates = GameManager.Instance.ConnectedPlayerStates;
        _teamStates = GameManager.Instance.ConnectedTeamStates;
        Refresh(_teamStates, _playerStates, _clientNetworkId);
    }

    private void Refresh(Dictionary<int, TeamState> teamStates, Dictionary<ulong, PlayerState> playerStates, 
        ulong clientNetworkId)
    {
        Init();
        foreach (var kvp in playerStates)
        {
            var playerState = kvp.Value;
            SpawnPlayer(teamStates[playerState.teamIndex], playerState, playerState.clientNetworkId==clientNetworkId);
        }

        var sessionLeaderId = SessionCache.GetJoinedSessionLeaderUserId();
        if (String.IsNullOrEmpty(sessionLeaderId)) return;
        var isStartBtnVisible = GameData.CachedPlayerState.playerId.Equals(sessionLeaderId);
        startButton.gameObject.SetActive(isStartBtnVisible);
    }

    private const string CountDownPrefix = "MATCH START IN: ";
    public void Countdown(int second)
    {
        ShowStatus(CountDownPrefix+second);
    }

    public void ShowStatus(string status)
    {
        if(!statusContainer.activeSelf)
            statusContainer.SetActive(true);
        statusLabel.text = status;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchLobbyMenuCanvas;
    }
}
