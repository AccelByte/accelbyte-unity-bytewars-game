using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSelectionMenu : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    private LeaderboardEssentialsWrapper _leaderboardWrapper;

    public static string chosenLeaderboardCode;
    public static Dictionary<string, string[]> leaderboardCycleIds;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        _leaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();

        DisplayLeaderboardList();
    }

    private void OnEnable()
    {
        if (!_leaderboardWrapper) return;

        DisplayLeaderboardList();
    }

    private void OnDisable()
    {
        leaderboardListPanel.DestroyAllChildren();
    }

    private void ChangeToLeaderboardCycleMenu(string newLeaderboardCode)
    {
        chosenLeaderboardCode = newLeaderboardCode;
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardCycleMenuCanvas);
    }

    private void OnGetLeaderboardListCompleted(Result<LeaderboardPagedListV3> result)
    {
        if (result.IsError) return;

        leaderboardCycleIds = new Dictionary<string, string[]>();
        foreach (LeaderboardDataV3 leaderboardData in result.Value.Data)
        {
            if (!leaderboardData.Name.Contains("Unity")) continue;

            Button leaderboardButton =
                Instantiate(leaderboardItemButtonPrefab, leaderboardListPanel).GetComponent<Button>();
            TMP_Text leaderboardButtonText = leaderboardButton.GetComponentInChildren<TMP_Text>();
            leaderboardButtonText.text = leaderboardData.Name.Replace("Unity Leaderboard ", "");

            leaderboardButton.onClick.AddListener(() => ChangeToLeaderboardCycleMenu(leaderboardData.LeaderboardCode));

            leaderboardCycleIds.Add(leaderboardData.LeaderboardCode, leaderboardData.CycleIds);
        }
    }

    private void DisplayLeaderboardList()
    {
        _leaderboardWrapper.GetLeaderboardList(OnGetLeaderboardListCompleted);
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
        return AssetEnum.LeaderboardSelectionMenuCanvas;
    }
}