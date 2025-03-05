// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverMenuCanvas : MenuCanvas
{
    [SerializeField] private TextMeshProUGUI winnerPlayerTextUI;
    [SerializeField] private RectTransform leaderboardList;
    [SerializeField] private LeaderboardEntryController leaderboardEntryPrefab;
    [SerializeField] private Button playAgainBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private RectTransform countdownContainer;
    [SerializeField] private TextMeshProUGUI countdownTxt;

    private void Start()
    {
        playAgainBtn.onClick.AddListener(OnPlayAgainButtonClicked);
        quitBtn.onClick.AddListener(OnQuitButtonClicked);
    }

    private void OnPlayAgainButtonClicked()
    {
        GameManager.Instance.RestartLocalGame();
    }

    private void OnQuitButtonClicked()
    {
        StartCoroutine(GameManager.Instance.QuitToMainMenu());
    }

    private void UpdateWinnerPlayerUI()
    {
        bool isLocalGame = GameManager.Instance.IsLocalGame;
        bool isTeamBasedGameMode = GameManager.Instance.InGameMode is
            InGameMode.MatchmakingTeamDeathmatch or InGameMode.CreateMatchTeamDeathmatch or InGameMode.LocalTeamDeathmatch;

        GameManager.Instance.GetWinner(out TeamState winnerTeam, out PlayerState winnerPlayer);

        // If no winner, then the game is draw.
        if (winnerTeam == null || winnerPlayer == null) 
        {
            winnerPlayerTextUI.text = "Game Is a Draw!";
            winnerPlayerTextUI.color = Color.white;
        }
        // Display team winner if the game mode is team based. Otherwise, display player winner name.
        else
        {
            bool isLocalPlayer = GameData.CachedPlayerState.PlayerId == winnerPlayer.PlayerId;

            string playerWinnerText = isLocalPlayer && !isLocalGame ? "You Win!" : $"{winnerPlayer.GetPlayerName()} Wins!";
            string teamWinnerText = $"Team {winnerTeam.teamIndex + 1} Wins!";

            winnerPlayerTextUI.text = isTeamBasedGameMode ? teamWinnerText : playerWinnerText;
            winnerPlayerTextUI.color = winnerTeam.teamColour;
        }
    }

    private void GenerateLeaderboardList()
    {
        TeamState[] teamStates = GameManager.Instance.TeamStates;

        List<PlayerState> playerStates = 
            GameManager.Instance.ConnectedPlayerStates.Values.OrderByDescending(p => p.Score).ThenBy(p => p.TeamIndex).ToList();

        foreach (PlayerState playerState in playerStates)
        {
            LeaderboardEntryController playerEntry = 
                Instantiate(leaderboardEntryPrefab, Vector3.zero, Quaternion.identity, leaderboardList);

            playerEntry.SetDetails(playerState.GetPlayerName(), teamStates[playerState.TeamIndex].teamColour, 
            playerState.KillCount, (int)playerState.Score);
            playerEntry.gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.InGameState == InGameState.GameOver)
        {
            UpdateWinnerPlayerUI();
            GenerateLeaderboardList();
        }

        playAgainBtn.gameObject.SetActive(!NetworkManager.Singleton.IsListening); 
    }

    private void OnDisable()
    {
        leaderboardList.DestroyAllChildren();
        winnerPlayerTextUI.color = Color.white;
    }

    public void Countdown(int countdownSecond)
    {
        countdownTxt.text = $"Quitting in: {countdownSecond}";
        countdownContainer.gameObject.SetActive(countdownSecond > 0);
    }

    #region MenuCanvas

    public override GameObject GetFirstButton()
    {
        return playAgainBtn.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.GameOverMenuCanvas;
    }

    #endregion
}
