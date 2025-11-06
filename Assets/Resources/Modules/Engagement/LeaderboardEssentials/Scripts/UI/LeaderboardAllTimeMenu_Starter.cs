// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardAllTimeMenu_Starter : MenuCanvas
{
    [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
    [SerializeField] private LeaderboardEntry playerLeaderboardEntry;
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;

    // TODO: Declare tutorial module variables here.

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.

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
        return AssetEnum.LeaderboardAllTimeMenu_Starter;
    }
}