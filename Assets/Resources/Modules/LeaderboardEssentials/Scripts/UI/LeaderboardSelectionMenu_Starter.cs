using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSelectionMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    // Copy wrapper initialization from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 2)


    // Copy chosenLeaderboardCode declaration from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 3)


    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Copy wrapper initialization from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 2)
        
        // Copy DisplayLeaderboardList() from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 7)
    }

    // Copy OnEnable() from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 7)


    private void OnDisable()
    {
        leaderboardListPanel.DestroyAllChildren();
    }

    // Copy ChangeToLeaderboardCycleMenu() from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 4)


    // Copy OnGetLeaderboardListCompleted() from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 5)


    // Copy DisplayLeaderboardList() from "Display Leaderboard List in Leaderboard Selection Menu" unit here (step number 6)


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
        return AssetEnum.LeaderboardSelectionMenuCanvas_Starter;
    }
}