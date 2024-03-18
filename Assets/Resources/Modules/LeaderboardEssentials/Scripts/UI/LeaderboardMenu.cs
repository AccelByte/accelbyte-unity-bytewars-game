using System;
using System.Collections;
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
    [SerializeField] private Transform placeholderText;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject rankingEntryPanelPrefab;
    [SerializeField] private RankingEntryPanel userRankingPanel;

    private TokenData currentUserData;

    private LeaderboardEssentialsWrapper leaderboardWrapper;
    private AuthEssentialsWrapper authWrapper;

    private string currentLeaderboardCode;
    private LeaderboardCycleMenu.LeaderboardCycleType currentCycleType;

    private const int QUERY_OFFSET = 0;
    private const int QUERY_LIMIT = 10;

    public delegate void LeaderboardMenuDelegate(LeaderboardMenu leaderboardMenu,
        UserCycleRanking[] userCycleRankings = null);

    public static event LeaderboardMenuDelegate onDisplayRankingListEvent = delegate { };
    public static event LeaderboardMenuDelegate onDisplayUserRankingEvent = delegate { };

    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        leaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        DisplayRankingList();
    }

    private void OnDisable()
    {
        placeholderText.gameObject.SetActive(true);
        rankingListPanel.DestroyAllChildren(placeholderText);
        userRankingPanel.ResetRankingEntry();
    }

    private void OnEnable()
    {
        if (!leaderboardWrapper || !authWrapper) return;

        DisplayRankingList();
    }

    private void InitializeLeaderboardRequiredValues()
    {
        currentUserData = authWrapper.userData;
        currentLeaderboardCode = LeaderboardSelectionMenu.chosenLeaderboardCode;
        currentCycleType = LeaderboardCycleMenu.chosenCycleType;
    }

    private void OnGetUserRankingCompleted(Result<UserRankingDataV3> result)
    {
        if (result.IsError) return;

        onDisplayUserRankingEvent.Invoke(this, result.Value.Cycles);

        if (currentCycleType != LeaderboardCycleMenu.LeaderboardCycleType.AllTime) return;

        UserRanking allTimeUserRank = result.Value.AllTime;
        userRankingPanel.SetRankingDetails(currentUserData.user_id, allTimeUserRank.rank,
            currentUserData.display_name, allTimeUserRank.point);
    }

    private void OnBulkGetUserInfoCompleted(Result<ListBulkUserInfoResponse> result,
        Dictionary<string, float> userRankInfos)
    {
        // userRankInfos: key = userId, value = displayName
        Dictionary<string, string> userDisplayNames =
            result.Value.data.ToDictionary(userInfo => userInfo.userId, userInfo => userInfo.displayName);
        int rankOrder = 0;
        foreach (string userId in userRankInfos.Keys)
        {
            rankOrder += 1;
            InstantiateRankingEntry(userId, rankOrder, userDisplayNames[userId], userRankInfos[userId]);
        }

        if (userRankInfos.Keys.Contains(currentUserData.user_id)) return;

        // Get player ranking if player not on the ranking list
        leaderboardWrapper.GetUserRanking(currentUserData.user_id, currentLeaderboardCode,
            OnGetUserRankingCompleted);
    }

    public void OnGetRankingsCompleted(Result<LeaderboardRankingResult> result)
    {
        if (result.IsError) return;

        // Set placeholder text active to true if list empty
        placeholderText.gameObject.SetActive(result.Value.data.Length <= 0);

        // Store the ranking result's userIds and points to a Dictionary
        Dictionary<string, float> userRankInfos =
            result.Value.data.ToDictionary(userPoint => userPoint.userId, userPoint => userPoint.point);

        // Get the players' display name from the provided user ids
        authWrapper.BulkGetUserInfo(userRankInfos.Keys.ToArray(),
            authResult => OnBulkGetUserInfoCompleted(authResult, userRankInfos));
    }

    private void DisplayRankingList()
    {
        InitializeLeaderboardRequiredValues();

        if (currentCycleType is LeaderboardCycleMenu.LeaderboardCycleType.AllTime)
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

        if (userId != currentUserData.user_id) return;

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