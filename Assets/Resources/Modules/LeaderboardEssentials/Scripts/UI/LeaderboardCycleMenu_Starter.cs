using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCycleMenu_Starter : MenuCanvas
{
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button backButton;

    // Copy leaderboard cycle type enum declaration from "Prepare Leaderboard UIs" unit here (step number 2)

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Copy allTimeButton onClick event listener from "Prepare Leaderboard UIs" unit here (step number 4)
    }
    
    // Copy ChangeToLeaderboardMenu() from "Prepare Leaderboard UIs" unit here (step number 3)

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    public override GameObject GetFirstButton()
    {
        return allTimeButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardCycleMenuCanvas_Starter;
    }
}