// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCycleMenu_Starter : MenuCanvas
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

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Put your code here
    }

    private void OnEnable()
    {
        // Put your code here
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
        return AssetEnum.LeaderboardCycleMenuCanvas_Starter;
    }
}