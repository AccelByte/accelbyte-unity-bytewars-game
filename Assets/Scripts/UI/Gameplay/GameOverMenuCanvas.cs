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

    private void UpdateWinnerPlayerUI(List<PlayerState> playerStates, TeamState[] teamStates, InGameMode gameMode)
    {
        switch (gameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode 
                    or InGameMode.CreateMatchDeathMatchGameMode
                    or InGameMode.Local4PlayerDeathMatchGameMode:
                
                Dictionary<int, float> teamScores = playerStates.GroupBy(p => p.teamIndex)
                               .ToDictionary(g => g.Key, g => g.Sum(p => p.score));
                float maxScore = teamScores.Values.Max();
                List<int> winningTeams = teamScores.Where(kv => kv.Value == maxScore).Select(kv => kv.Key).ToList();
                List<int> drawTeams = teamScores.Where(kv => kv.Value == maxScore && winningTeams.Count > 1)
                                        .Select(kv => kv.Key).ToList();
                
                if (drawTeams.Count > 1)
                {
                    winnerPlayerTextUI.text = "Game Draw!";
                }
                else
                {
                    TeamState teamWinner = teamStates[winningTeams[0]];
                    winnerPlayerTextUI.text = $"Team {teamWinner.teamIndex+1} Wins!";
                    winnerPlayerTextUI.color = teamWinner.teamColour;
                }

                break;
            default:
                maxScore = playerStates.Max(p => p.score);
                List<PlayerState> winners = playerStates.Where(p => p.score == maxScore).ToList();
                PlayerState winner = winners[0];
                bool isDraw = winners.Count > 1;
                
                if (isDraw)
                {
                    winnerPlayerTextUI.text = "Game Draw!";
                } 
                else
                {
                    winnerPlayerTextUI.text = GameData.CachedPlayerState.playerName == winner.GetPlayerName() ? "You Win!" : $"{winner.GetPlayerName()} Wins!";
                    winnerPlayerTextUI.color = teamStates[winners[0].teamIndex].teamColour;
                }

                break;
        }
    }

    private void GenerateLeaderboardList(List<PlayerState> playerStates, TeamState[] teamStates)
    {
        foreach (PlayerState playerState in playerStates)
        {
            LeaderboardEntryController playerEntry = Instantiate(
                leaderboardEntryPrefab, 
                Vector3.zero, 
                Quaternion.identity,
                leaderboardList);

                playerEntry.SetDetails(playerState.GetPlayerName(), teamStates[playerState.teamIndex].teamColour, 
                playerState.killCount, (int)playerState.score);
                playerEntry.gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.InGameState == InGameState.GameOver)
        {
            InGameMode gameMode = GameManager.Instance.InGameMode;
            List<PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates.Values.ToList();
            TeamState[] teamStates = GameManager.Instance.TeamStates;
            playerStates.OrderByDescending(p => p.score).ThenBy(p => p.teamIndex);
            UpdateWinnerPlayerUI(playerStates, teamStates, gameMode);
            GenerateLeaderboardList(playerStates, teamStates);
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
        if(!countdownContainer.gameObject.activeSelf) 
        {
            countdownContainer.gameObject.SetActive(true);
        }
            
        countdownTxt.text = $"Quitting in: {countdownSecond}";

        if (countdownSecond <= 0)
        {
            countdownContainer.gameObject.SetActive(false);
        }
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
