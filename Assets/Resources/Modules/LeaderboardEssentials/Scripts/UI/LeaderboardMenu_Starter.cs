// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
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
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject rankingEntryPanelPrefab;
    [SerializeField] private RankingEntryPanel userRankingPanel;

    [SerializeField] private Transform resultPanel;
    [SerializeField] private Transform emptyPanel;
    [SerializeField] private Transform loadingPanel;
    [SerializeField] private Transform loadingFailed;

    public enum LeaderboardMenuView
    {
        Default,
        Loading,
        Empty,
        Failed
    }

    private LeaderboardMenuView currentView = LeaderboardMenuView.Default;

    public LeaderboardMenuView CurrentView
    {
        get => currentView;
        set
        {
            resultPanel.gameObject.SetActive(value == LeaderboardMenuView.Default);
            emptyPanel.gameObject.SetActive(value == LeaderboardMenuView.Empty);
            loadingPanel.gameObject.SetActive(value == LeaderboardMenuView.Loading);
            loadingFailed.gameObject.SetActive(value == LeaderboardMenuView.Failed);
            currentView = value;
        }
    }

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
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
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardMenuCanvas_Starter;
    }
}