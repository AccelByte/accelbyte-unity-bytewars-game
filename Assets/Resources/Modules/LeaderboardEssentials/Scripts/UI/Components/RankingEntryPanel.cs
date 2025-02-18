// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingEntryPanel : MonoBehaviour
{
    [SerializeField] private Image prefabImage;
    [SerializeField] private TMP_Text playerRankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerScoreText;

    private const string DefaultDisplayNamePrefix = "PLAYER-";
    
    public void ResetRankingEntry()
    {
        playerRankText.text = "";
        playerNameText.text = "You Have No Ranking Yet";
        playerScoreText.text = "0";
    }
    
    public void SetRankingDetails(string userId, int playerRank, string playerName, float playerScore)
    {
        // If display name is null or empty, set to default format: "PLAYER-<<5 char of userId>>"
        if (string.IsNullOrEmpty(playerName)) playerName = $"{DefaultDisplayNamePrefix}{userId[..5]}";
        
        playerRankText.text = $"#{playerRank}";
        playerNameText.text = playerName;
        playerScoreText.text = $"{playerScore}";
    }

    public void SetPanelColor(Color color)
    {
        prefabImage.color = color;
    }
}
