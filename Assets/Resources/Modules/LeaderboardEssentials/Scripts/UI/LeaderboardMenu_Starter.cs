using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform rankingListPanel;
    [SerializeField] private Transform placeholderText;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject rankingEntryPanelPrefab;
    [SerializeField] private RankingEntryPanel userRankingPanel;

    private TokenData _currentUserData;
    
    // Copy wrapper class local variables declaration from "Display Ranking in Leaderboard Menu" here (step number 2)


    // Copy required local variables declaration from "Display Ranking in Leaderboard Menu" here (step number 3)


    // Copy constant variables declaration from "Display Ranking in Leaderboard Menu" here (step number 5)


    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Copy wrapper class local variables initialization from "Display Ranking in Leaderboard Menu" here (step number 2)


        // Copy DisplayRankingList() from "Display Ranking in Leaderboard Menu" here (step number 10)

    }

    private void OnDisable()
    {
        placeholderText.gameObject.SetActive(true);
        rankingListPanel.DestroyAllChildren(placeholderText);
        userRankingPanel.ResetRankingEntry();
    }

    // Copy OnEnable() from "Display Ranking in Leaderboard Menu" here (step number 10)


    // Copy InitializeLeaderboardRequiredValues() from "Display Ranking in Leaderboard Menu" here (step number 4)


    // Copy OnGetUserRankingCompleted() from "Display Ranking in Leaderboard Menu" here (step number 6)


    // Copy OnBulkGetUserInfoCompleted() from "Display Ranking in Leaderboard Menu" here (step number 7)


    // Copy OnGetRankingsCompleted() from "Display Ranking in Leaderboard Menu" here (step number 8)

    
    // Copy DisplayRankingList() from "Display Ranking in Leaderboard Menu" here (step number 9)


    public void InstantiateRankingEntry(string userId, int playerRank, string playerName, float playerScore)
    {
        RankingEntryPanel rankingEntryPanel =
            Instantiate(rankingEntryPanelPrefab, rankingListPanel).GetComponent<RankingEntryPanel>();
        rankingEntryPanel.SetRankingDetails(userId, playerRank, playerName, playerScore);

        if (userId != _currentUserData.user_id) return;

        // Highlight players ranking entry and set ranking details to user ranking panel
        rankingEntryPanel.SetPanelColor(new Color(1.0f, 1.0f, 1.0f, 0.098f)); // rgba 255,255,255,25
        userRankingPanel.SetRankingDetails(userId, playerRank, playerName, playerScore);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardMenuCanvas_Starter;
    }
}