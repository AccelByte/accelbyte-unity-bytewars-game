// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSelectionMenu : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Transform loadingPanel;
    [SerializeField] private Transform loadingFailed;

    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    private enum LeaderboardSelectionView
    {
        Default,
        Loading,
        Failed
    }

    private LeaderboardSelectionView currentView = LeaderboardSelectionView.Default;

    private LeaderboardSelectionView CurrentView
    {
        get => currentView;
        set
        {
            leaderboardListPanel.gameObject.SetActive(value == LeaderboardSelectionView.Default);
            loadingPanel.gameObject.SetActive(value == LeaderboardSelectionView.Loading);
            loadingFailed.gameObject.SetActive(value == LeaderboardSelectionView.Failed);
            currentView = value;
        }
    }

    private LeaderboardEssentialsWrapper leaderboardWrapper;

    public static string chosenLeaderboardCode;
    public static Dictionary<string, string[]> leaderboardCycleIds = new Dictionary<string, string[]>();

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        leaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();

        if (!leaderboardWrapper)
        {
            return;
        }

        DisplayLeaderboardList();
    }

    private void ChangeToLeaderboardCycleMenu(string newLeaderboardCode)
    {
        chosenLeaderboardCode = newLeaderboardCode;
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardCycleMenuCanvas);
    }

    private void DisplayLeaderboardList()
    {
        CurrentView = LeaderboardSelectionView.Loading;
        leaderboardWrapper.GetLeaderboardList(OnGetLeaderboardListCompleted);
    }

    private void OnGetLeaderboardListCompleted(Result<LeaderboardPagedListV3> result)
    {
        if (result.IsError) 
        {
            CurrentView = LeaderboardSelectionView.Failed;
            return;
        }

        LeaderboardDataV3[] leadeboardList = result.Value.Data.Where(data => data.Name.Contains("Unity")).ToArray();

        // No relevant leaderboard was found.
        if (leadeboardList.Length < 0)
        {
            CurrentView = LeaderboardSelectionView.Failed;
            return;
        }

        // Show leaderboard list.
        leaderboardListPanel.DestroyAllChildren();
        leaderboardCycleIds.Clear();
        foreach (LeaderboardDataV3 leaderboard in leadeboardList)
        {
            Button leaderboardButton =
                Instantiate(leaderboardItemButtonPrefab, leaderboardListPanel).GetComponent<Button>();
            TMP_Text leaderboardButtonText = leaderboardButton.GetComponentInChildren<TMP_Text>();
            leaderboardButtonText.text = leaderboard.Name.Replace("Unity Leaderboard ", "");

            leaderboardButton.onClick.AddListener(() => ChangeToLeaderboardCycleMenu(leaderboard.LeaderboardCode));

            leaderboardCycleIds.Add(leaderboard.LeaderboardCode, leaderboard.CycleIds);
        }

        CurrentView = LeaderboardSelectionView.Default;
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