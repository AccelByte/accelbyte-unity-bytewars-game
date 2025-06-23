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

public class LeaderboardAllTimeMenu : MenuCanvas
{
    [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
    [SerializeField] private LeaderboardEntry playerLeaderboardEntry;
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;

    private string LeaderboardCode => 
        LeaderboardEssentialsModels.GetLeaderboardCodeByGameMode(LeaderboardsMenu.SelectedGameMode);

    private LeaderboardEssentialsWrapper leaderboardWrapper;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        leaderboardWrapper ??= TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();
        if (leaderboardWrapper)
        {
            DisplayRankingList();
        }
    }

    private void DisplayRankingList()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        leaderboardListPanel.DestroyAllChildren();

        // Get leaderboard ranking list.
        leaderboardWrapper.GetRankings(
            LeaderboardCode, 
            (Result<LeaderboardRankingResult> result) =>
            {
                // The backend treats an empty leaderboard as an error. Therefore, display the empty state instead.
                if (result.IsError || result.Value.data.Length <= 0)
                {
                    BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
                    widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
                    return;
                }

                // Query the users info in the leaderboard rankings.
                UserPoint[] rankings = result.Value.data;
                AccelByteSDK.GetClientRegistry().GetApi().GetUser().GetUserOtherPlatformBasicPublicInfo(
                    "ACCELBYTE",
                    result.Value.data.Select(x => x.userId).ToArray(),
                    (Result<AccountUserPlatformInfosResponse> usersInfoResult) =>
                    {
                        // Abort if failed to query users info.
                        if (usersInfoResult.IsError)
                        {
                            BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
                            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                            return;
                        }

                        // Generate leaderboard ranking entries.
                        int rankOrder = 0;
                        bool isCurrentPlayerInRanked = false;
                        Dictionary<string, AccountUserPlatformData> usersInfo = usersInfoResult.Value.Data.ToDictionary(x => x.UserId, x => x);
                        foreach(UserPoint ranking in rankings)
                        {
                            LeaderboardEntry entry = Instantiate(leaderboardEntryPrefab, leaderboardListPanel).GetComponent<LeaderboardEntry>();
                            usersInfo.TryGetValue(ranking.userId, out AccountUserPlatformData userInfo);
                            entry.SetRankingDetails(ranking.userId, ++rankOrder, userInfo?.DisplayName, ranking.point);

                            // If the user is the current logged-in player, display the rank on the player ranking entry card.
                            if (ranking.userId == GameData.CachedPlayerState.PlayerId)
                            {
                                isCurrentPlayerInRanked = true;
                                DisplayPlayerRanking(new UserRanking()
                                {
                                    rank = rankOrder,
                                    point = ranking.point
                                });
                            }
                        }

                        // Get the logged-in player's rank if it is not included in the leaderboard.
                        if (!isCurrentPlayerInRanked)
                        {
                            leaderboardWrapper.GetUserRanking(
                                GameData.CachedPlayerState.PlayerId,
                                LeaderboardCode,
                                (Result<UserRankingDataV3> userRankResult) =>
                                {
                                    // Display the leaderboard result.
                                    DisplayPlayerRanking(userRankResult.IsError ? null : userRankResult.Value.AllTime);
                                    widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
                                });
                            return;
                        }

                        // Display the leaderboard result.
                        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
                    });
            },
            0, LeaderboardEssentialsModels.QueryRankingsLimit);
    }

    private void DisplayPlayerRanking(UserRanking playerRanking)
    {
        // If current logged-in player is not ranked, display empty entry. Else, display the rank.
        if (playerRanking == null)
        {
            playerLeaderboardEntry.Reset();
            return;
        }
        playerLeaderboardEntry.SetRankingDetails(
            GameData.CachedPlayerState.PlayerId,
            playerRanking.rank,
            LeaderboardEssentialsModels.RankedMessage,
            playerRanking.point);
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
        return AssetEnum.LeaderboardAllTimeMenu;
    }
}