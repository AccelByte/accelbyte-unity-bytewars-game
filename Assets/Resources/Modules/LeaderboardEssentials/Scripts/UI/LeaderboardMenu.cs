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

public class LeaderboardMenu : MenuCanvas
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

    private PlayerState currentUserData;

    private LeaderboardEssentialsWrapper leaderboardWrapper;
    private AuthEssentialsWrapper authWrapper;

    private string currentLeaderboardCode;
    private LeaderboardCycleMenu.LeaderboardCycleType currentCycleType;

    private const int QUERY_OFFSET = 0;
    private const int QUERY_LIMIT = 10;

    public delegate void LeaderboardMenuDelegate(LeaderboardMenu leaderboardMenu,
        UserCycleRanking[] userCycleRankings = null);

    public static event LeaderboardMenuDelegate onDisplayRankingListEvent = delegate { };

    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        leaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        if (!leaderboardWrapper || !authWrapper)
        {
            return;
        }

        DisplayRankingList();
    }

    private void InitializeLeaderboardRequiredValues()
    {
        currentUserData = GameData.CachedPlayerState;
        currentLeaderboardCode = LeaderboardSelectionMenu.chosenLeaderboardCode;
        currentCycleType = LeaderboardCycleMenu.chosenCycleType;
    }

    private void OnGetUserRankingCompleted(Result<UserRankingDataV3> result)
    {
        if (result.IsError)
        {
            // This block prevents leaderboard display issues caused by users without rankings
            if (result.Error.Code == ErrorCode.LeaderboardRankingUnableToRetrieve)
            {
                BytewarsLogger.LogWarning($"Failed to Get User Ranking. Error: {result.Error.Message}");
                CurrentView = LeaderboardMenuView.Default;
                return;
            }
            else
            {
                BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
                CurrentView = LeaderboardMenuView.Failed;
                return;
            } 
        }

        if (currentCycleType == LeaderboardCycleMenu.LeaderboardCycleType.AllTime)
        {
            UserRanking allTimeUserRank = result.Value.AllTime;
            if (allTimeUserRank != null)
            {
                userRankingPanel.SetRankingDetails(
                    currentUserData.playerId,
                    allTimeUserRank.rank,
                    currentUserData.playerName,
                    allTimeUserRank.point);
            }
        }
        else
        {
            UserCycleRanking cycleUserRank = result.Value.Cycles.First(data => data.CycleId.Equals(LeaderboardCycleMenu.chosenCycleId));
            if (cycleUserRank != null)
            {
                userRankingPanel.SetRankingDetails(
                    currentUserData.playerId,
                    cycleUserRank.Rank,
                    currentUserData.playerName,
                    cycleUserRank.Point);
            }
        }

        CurrentView = LeaderboardMenuView.Default;
    }

    private void OnBulkGetUserInfoCompleted(Result<ListBulkUserInfoResponse> result, Dictionary<string, float> userRankInfos)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
            CurrentView = LeaderboardMenuView.Failed;
            return;
        }

        // Populate leaderboard ranking entries.
        int rankOrder = 0;
        Dictionary<string, string> userDisplayNames = 
            result.Value.data.ToDictionary(userInfo => userInfo.userId, userInfo => userInfo.displayName);
        foreach (string userId in userRankInfos.Keys)
        {
            rankOrder += 1;
            InstantiateRankingEntry(userId, rankOrder, userDisplayNames[userId], userRankInfos[userId]);

            if (userId.Equals(currentUserData.playerName))
            {
                userRankingPanel.SetRankingDetails(
                    userId,
                    rankOrder,
                    userDisplayNames[userId],
                    userRankInfos[userId]);
            }
        }

        /* No need to query current player ranking if already in the ranking list.
         * Immediately show the leaderboard ranking list. */
        if (userRankInfos.Keys.Contains(currentUserData.playerId))
        {
            CurrentView = LeaderboardMenuView.Default;
            return;
        }

        // Get current player ranking if not already in the ranking list.
        leaderboardWrapper.GetUserRanking(
            currentUserData.playerId,
            currentLeaderboardCode,
            OnGetUserRankingCompleted);
    }

    public void OnGetRankingsCompleted(Result<LeaderboardRankingResult> result)
    {
        /* The backend returns result as error even if leaderboard ranking is empty.
         * Hence, here the game show the leaderboard empty message. */
        if (result.IsError || result.Value.data.Length <= 0)
        {
            BytewarsLogger.LogWarning($"Failed to display leaderboard rankings. Error: {result.Error.Message}");
            CurrentView = LeaderboardMenuView.Empty;
            return;
        }

        // Store the ranking result's userIds and points to a Dictionary
        Dictionary<string, float> userRankInfos =
            result.Value.data.ToDictionary(userPoint => userPoint.userId, userPoint => userPoint.point);

        // Get the players' display name from the provided user ids
        authWrapper.BulkGetUserInfo(
            userRankInfos.Keys.ToArray(),
            authResult => OnBulkGetUserInfoCompleted(authResult, userRankInfos));
    }

    private void DisplayRankingList()
    {
        CurrentView = LeaderboardMenuView.Loading;
        rankingListPanel.DestroyAllChildren();
        userRankingPanel.ResetRankingEntry();

        InitializeLeaderboardRequiredValues();

        if (currentCycleType == LeaderboardCycleMenu.LeaderboardCycleType.AllTime)
        {
            leaderboardWrapper.GetRankings(currentLeaderboardCode, OnGetRankingsCompleted, QUERY_OFFSET, QUERY_LIMIT);
        }

        onDisplayRankingListEvent.Invoke(this);
    }

    public void InstantiateRankingEntry(string userId, int playerRank, string playerName, float playerScore)
    {
        RankingEntryPanel rankingEntryPanel =
            Instantiate(rankingEntryPanelPrefab, rankingListPanel).GetComponent<RankingEntryPanel>();
        rankingEntryPanel.SetRankingDetails(userId, playerRank, playerName, playerScore);

        if (userId != currentUserData.playerId) return;

        // Highlight players ranking entry and set ranking details to user ranking panel
        rankingEntryPanel.SetPanelColor(new Color(1.0f, 1.0f, 1.0f, 0.098f)); // rgba 255,255,255,25
        userRankingPanel.SetRankingDetails(userId, playerRank, playerName, playerScore);
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
        return AssetEnum.LeaderboardMenuCanvas;
    }
}