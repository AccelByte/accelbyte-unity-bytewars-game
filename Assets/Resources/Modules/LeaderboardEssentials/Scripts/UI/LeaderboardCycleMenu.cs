// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCycleMenu : MenuCanvas
{
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button backButton;

    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Transform loadingPanel;
    [SerializeField] private Transform loadingFailed;

    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    public enum LeaderboardCycleView
    {
        Default,
        Loading,
        Failed
    }

    private LeaderboardCycleView currentView = LeaderboardCycleView.Default;

    public LeaderboardCycleView CurrentView
    {
        get => currentView;
        set
        {
            leaderboardListPanel.gameObject.SetActive(value == LeaderboardCycleView.Default);
            loadingPanel.gameObject.SetActive(value == LeaderboardCycleView.Loading);
            loadingFailed.gameObject.SetActive(value == LeaderboardCycleView.Failed);
            currentView = value;
        }
    }

    public static LeaderboardCycleType chosenCycleType;
    public static string chosenCycleId;

    public enum LeaderboardCycleType
    {
        AllTime,
        Weekly
    }

    public delegate void LeaderboardCycleMenuDelegate(
        LeaderboardCycleMenu leaderboardCycleMenu,
        Transform leaderboardListPanel,
        GameObject leaderboardItemButtonPrefab);

    public static event LeaderboardCycleMenuDelegate OnLeaderboardCycleMenuActivated = delegate { };

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
        allTimeButton.onClick.AddListener(() => ChangeToLeaderboardMenu(LeaderboardCycleType.AllTime, string.Empty));
    }

    private void OnEnable()
    {
        leaderboardListPanel.DestroyAllChildren(allTimeButton.transform);
        CurrentView = LeaderboardCycleView.Default;
        
        if (ApiClientHelper.IsPlayerLoggedIn)
        {
            OnLeaderboardCycleMenuActivated.Invoke(this, leaderboardListPanel, leaderboardItemButtonPrefab);
        }
    }

    public static void ChangeToLeaderboardMenu(LeaderboardCycleType cycleType, string cycleId)
    {
        chosenCycleType = cycleType;
        chosenCycleId = cycleId;

        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardMenuCanvas);
    }

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
        return AssetEnum.LeaderboardCycleMenuCanvas;
    }
}