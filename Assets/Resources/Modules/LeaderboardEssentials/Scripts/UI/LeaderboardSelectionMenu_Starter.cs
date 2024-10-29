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

public class LeaderboardSelectionMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Transform noEntryPanel;
    [SerializeField] private Transform loadingPanel;
    [SerializeField] private Transform failedPanel;

    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    private enum LeaderboardSelectionView
    {
        Default,
        Empty,
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
            noEntryPanel.gameObject.SetActive(value == LeaderboardSelectionView.Empty);
            loadingPanel.gameObject.SetActive(value == LeaderboardSelectionView.Loading);
            failedPanel.gameObject.SetActive(value == LeaderboardSelectionView.Failed);
            currentView = value;
        }
    }

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    private void Start()
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
        return AssetEnum.LeaderboardSelectionMenuCanvas_Starter;
    }
}