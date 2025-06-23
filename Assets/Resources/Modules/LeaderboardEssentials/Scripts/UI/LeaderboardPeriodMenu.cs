// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPeriodMenu : MenuCanvas
{
    public static StatisticCycleType SelectedPeriod { get; private set; }

    [SerializeField] private ButtonAnimation periodButtonPrefab;
    [SerializeField] private Transform periodPanel;
    [SerializeField] private Button backButton;

    private StatisticCycleType[] leaderboardPeriods =
    {
        StatisticCycleType.None,
        StatisticCycleType.Weekly
    };

    private ModuleModel leaderboardEssentials, periodicLeaderboard;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        leaderboardEssentials ??= TutorialModuleManager.Instance.GetModule(TutorialType.LeaderboardEssentials);
        periodicLeaderboard ??= TutorialModuleManager.Instance.GetModule(TutorialType.PeriodicLeaderboardEssentials);

        // Generate periodic leaderboard option buttons.
        if (periodPanel.childCount != leaderboardPeriods.Length)
        {
            periodPanel.DestroyAllChildren();
            foreach (StatisticCycleType period in leaderboardPeriods)
            {
                // Skip periodic leaderboard if the module is not active.
                if (period != StatisticCycleType.None && (periodicLeaderboard == null || !periodicLeaderboard.isActive))
                {
                    continue;
                }

                ButtonAnimation periodButton = Instantiate(periodButtonPrefab, periodPanel).GetComponent<ButtonAnimation>();
                periodButton.text.text = period == StatisticCycleType.None ? "All Time" : period.ToString();
                periodButton.button.onClick.AddListener(() => ChangeToLeaderboardMenu(period));
            }
        }
    }

    private void ChangeToLeaderboardMenu(StatisticCycleType selectedPeriod)
    {
        SelectedPeriod = selectedPeriod;

        // Open all-time leaderboard menu.
        if (selectedPeriod == StatisticCycleType.None)
        {
            MenuManager.Instance.ChangeToMenu(leaderboardEssentials.isStarterActive ? AssetEnum.LeaderboardAllTimeMenu_Starter: AssetEnum.LeaderboardAllTimeMenu);
        }
        // Open periodic leaderboard menu.
        else
        {
            MenuManager.Instance.ChangeToMenu(periodicLeaderboard.isStarterActive ? AssetEnum.PeriodicLeaderboardMenu_Starter : AssetEnum.PeriodicLeaderboardMenu);
        }
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return periodPanel.childCount > 0 ? periodPanel.GetChild(0).gameObject : backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardPeriodMenu;
    }
}