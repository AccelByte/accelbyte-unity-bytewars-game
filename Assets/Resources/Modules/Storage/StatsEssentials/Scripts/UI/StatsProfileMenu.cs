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

public class StatsProfileMenu : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private StatsProfileEntry statsEntryPrefab;
    [SerializeField] private Transform singlePlayerStatsPanel;
    [SerializeField] private Transform eliminationPlayerStatsPanel;
    [SerializeField] private Transform teamDeathmatchPlayerStatsPanel;
    [SerializeField] private Button backButton;

    private List<(GameStatsData statData, Transform panel)> statsPanelList;

    private StatsEssentialsWrapper statsWrapper;
    
    void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    void OnEnable()
    {
        statsWrapper ??= TutorialModuleManager.Instance.GetModuleClass<StatsEssentialsWrapper>();
        statsPanelList ??= new()
        {
            (SinglePlayerStatsData, singlePlayerStatsPanel),
            (EliminationStatsData, eliminationPlayerStatsPanel),
            (TeamDeathmatchStatsData, teamDeathmatchPlayerStatsPanel)
        };

        if (statsWrapper != null)
        {
            DisplayStats();
        }
    }

    private void DisplayStats()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        string[] statCodes = statsPanelList.SelectMany(x => x.statData.StatsCodes).ToArray();
        statsWrapper.GetUserStatsFromClient(statCodes, null, OnGetUserStatsCompleted);
    }

    private void OnGetUserStatsCompleted(Result<PagedStatItems> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        if (result.Value.data.Length <= 0)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
            return;
        }

        // Generate entries to display statistics in groups.
        StatItem[] statItems = result.Value.data;
        statsPanelList.Select(x => x.panel).ToList().ForEach(x => x.DestroyAllChildren());
        foreach((GameStatsData statData, Transform panel) statsPanel in statsPanelList)
        {
            foreach(GameStatsData.GameStatsModel model in statsPanel.statData.StatsModels)
            {
                StatItem statItem = statItems.FirstOrDefault(x => x.statCode == model.StatCode);
                StatsProfileEntry entry = Instantiate(statsEntryPrefab, statsPanel.panel).GetComponent<StatsProfileEntry>();
                entry.Setup(model.DisplayName, statItem != null ? (int)statItem.value : 0);
            }
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
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
        return AssetEnum.StatsProfileMenu;
    }
}
