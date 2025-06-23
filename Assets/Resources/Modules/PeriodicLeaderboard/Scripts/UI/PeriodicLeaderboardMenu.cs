// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class PeriodicLeaderboardMenu : MenuCanvas
{
    [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
    [SerializeField] private LeaderboardEntry playerLeaderboardEntry;
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;

    private string LeaderboardCode => 
        LeaderboardEssentialsModels.GetLeaderboardCodeByGameMode(LeaderboardsMenu.SelectedGameMode);

    private string PeriodName => 
        LeaderboardEssentialsModels.GetFormattedLeaderboardPeriod(LeaderboardPeriodMenu.SelectedPeriod);

    private PeriodicLeaderboardWrapper periodicLeaderboard;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        periodicLeaderboard ??= TutorialModuleManager.Instance.GetModuleClass<PeriodicLeaderboardWrapper>();
        if (periodicLeaderboard)
        {
            DisplayRankingList();
        }
    }

    private void DisplayRankingList()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        leaderboardListPanel.DestroyAllChildren();

        // Get statistics cycle config by selected period.
        periodicLeaderboard.GetStatCycleConfigByName(PeriodName, (result) =>
        {
            // Abort if the period is not found.
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
                widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                return;
            }

            // Get leaderboard ranking list by statistics cycle.
            string cycleId = result.Value.Id;
            periodicLeaderboard.GetRankingsByCycle(
                LeaderboardCode,
                cycleId,
                (Result<LeaderboardRankingResult> rankingResult) =>
                {
                    // The backend treats an empty leaderboard as an error. Therefore, display the empty state instead.
                    if (rankingResult.IsError || rankingResult.Value.data.Length <= 0)
                    {
                        BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {rankingResult.Error.Message}");
                        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
                        return;
                    }

                    // Query the users info in the leaderboard rankings.
                    UserPoint[] rankings = rankingResult.Value.data;
                    AccelByteSDK.GetClientRegistry().GetApi().GetUser().GetUserOtherPlatformBasicPublicInfo(
                        "ACCELBYTE",
                        rankingResult.Value.data.Select(x => x.userId).ToArray(),
                        (Result<AccountUserPlatformInfosResponse> usersInfoResult) =>
                        {
                            // Abort if failed to query users info.
                            if (usersInfoResult.IsError)
                            {
                                BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {rankingResult.Error.Message}");
                                widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                                return;
                            }

                            // Generate leaderboard ranking entries.
                            int rankOrder = 0;
                            bool isCurrentPlayerInRanked = false;
                            Dictionary<string, AccountUserPlatformData> usersInfo = 
                                usersInfoResult.Value.Data.ToDictionary(x => x.UserId, x => x);
                            foreach (UserPoint ranking in rankings)
                            {
                                LeaderboardEntry entry = Instantiate(leaderboardEntryPrefab, leaderboardListPanel).GetComponent<LeaderboardEntry>();
                                usersInfo.TryGetValue(ranking.userId, out AccountUserPlatformData userInfo);
                                entry.SetRankingDetails(ranking.userId, ++rankOrder, userInfo?.DisplayName, ranking.point);

                                // If the user is the current logged-in player, display the rank on the player ranking entry card.
                                if (ranking.userId == GameData.CachedPlayerState.PlayerId)
                                {
                                    isCurrentPlayerInRanked = true;
                                    DisplayPlayerRanking(new UserCycleRanking()
                                    {
                                        Rank = rankOrder,
                                        Point = ranking.point,
                                        CycleId = cycleId
                                    });
                                }
                            }

                            // Get the logged-in player's rank if it is not included in the leaderboard.
                            if (!isCurrentPlayerInRanked)
                            {
                                periodicLeaderboard.GetUserRanking(
                                    GameData.CachedPlayerState.PlayerId,
                                    LeaderboardCode,
                                    (Result<UserRankingDataV3> userRankResult) =>
                                    {
                                        // Display the leaderboard result.
                                        DisplayPlayerRanking(userRankResult.IsError ? null : userRankResult.Value.Cycles.FirstOrDefault(x => x.CycleId == cycleId));
                                        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
                                    });
                                return;
                            }

                            // Display the leaderboard result.
                            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
                        });
                },
                0, LeaderboardEssentialsModels.QueryRankingsLimit);
        });
    }

    private void DisplayPlayerRanking(UserCycleRanking playerCycleRanking)
    {
        // If current logged-in player is not ranked, display empty entry. Else, display the rank.
        if (playerCycleRanking == null)
        {
            playerLeaderboardEntry.Reset();
            return;
        }
        playerLeaderboardEntry.SetRankingDetails(
            GameData.CachedPlayerState.PlayerId,
            playerCycleRanking.Rank,
            LeaderboardEssentialsModels.RankedMessage,
            playerCycleRanking.Point);
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
        return AssetEnum.PeriodicLeaderboardMenu;
    }
}