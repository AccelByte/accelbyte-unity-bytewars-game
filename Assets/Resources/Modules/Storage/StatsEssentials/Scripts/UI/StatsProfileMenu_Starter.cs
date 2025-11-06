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
using static StatsEssentialsModels;

public class StatsProfileMenu_Starter : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private StatsProfileEntry statsEntryPrefab;
    [SerializeField] private Transform singlePlayerStatsPanel;
    [SerializeField] private Transform eliminationPlayerStatsPanel;
    [SerializeField] private Transform teamDeathmatchPlayerStatsPanel;
    [SerializeField] private Button backButton;

    private List<(GameStatsData statData, Transform panel)> statsPanelList;

    // TODO: Declare tutorial module variables here.

    void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.

    private void OnBackButtonClicked(){
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.StatsProfileMenu_Starter;
    }
}
