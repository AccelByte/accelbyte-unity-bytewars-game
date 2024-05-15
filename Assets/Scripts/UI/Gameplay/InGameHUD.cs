// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class InGameHUD : MonoBehaviour
{
    #region Fields and Properties

    [SerializeField] private PlayerHUD[] _playerHUDs;
    [SerializeField] private TextMeshProUGUI _timeLabel;

    public TextMeshProUGUI gameStatusText;
    public RectTransform gameStatusContainer;

    #endregion

    #region Lifecycle Methods

    private void OnEnable()
    {
        SetVisible(true);
    }

    #endregion

    #region Initialization Methods

    public void Init(TeamState[] teamStates, PlayerState[] playerStates)
    {
        foreach (TeamState teamState in teamStates)
        {
            var hud = _playerHUDs[teamState.teamIndex];
            hud.Init(teamState, playerStates);
        }
    }

    #endregion

    #region UI Update Methods

    public void SetTime(int timeInSecond)
    {
        _timeLabel.text = timeInSecond.ToString();
    }

    public void UpdateKillsAndScore(PlayerState playerState, PlayerState[] players)
    {
        PlayerHUD hud = _playerHUDs[playerState.teamIndex];
        
        List<PlayerState> teamPlayers = players.Where(p => p.teamIndex == playerState.teamIndex).ToList();
        
        int killCount = teamPlayers.Sum(p => p.killCount);
        int score = (int)teamPlayers.Sum(p => p.score);
        
        hud.SetKillsValue(killCount);
        hud.SetScoreValue(score);
    }

    public void SetLivesValue(int teamIndex, int lives)
    {
        _playerHUDs[teamIndex].SetLivesValue(lives);
    }

    public void UpdatePreGameCountdown(int second)
    {
        if (!gameStatusContainer.gameObject.activeSelf)
        {
            gameStatusContainer.gameObject.SetActive(true);
        }

        if (second == 0)
        {
            gameStatusText.text = "Game Started";
            HideGameStatusContainer();
        }
        else
        {
            gameStatusText.text = second.ToString();
        }
    }
    
    public void UpdateShutdownCountdown(string prefix, int second)
    {
        if (!gameStatusContainer.gameObject.activeSelf)
        {
            gameStatusContainer.gameObject.SetActive(true);
        }

        gameStatusText.text = prefix + second;

        if (second == 0)
        {
           HideGameStatusContainer();
        }
    }

    #endregion

    #region Reset and Visibility Methods

    public void Reset()
    {
        _timeLabel.text = "0";

        foreach (PlayerHUD playerHUD in _playerHUDs)
        {
            playerHUD.Reset();
        }

        HideGameStatusContainer();
        SetVisible(true);
    }

    public void HideGameStatusContainer()
    {
        gameStatusContainer.gameObject.SetActive(false);
    }

    public void SetVisible(bool isVisible)
    {
        GetComponent<Canvas>().enabled = isVisible;
    }

    #endregion
}