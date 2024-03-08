using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class StatsHelper : MonoBehaviour
{
    // statcodes' name configured in Admin Portal
    private const string SINGLEPLAYER_STATCODE = "unity-highestscore-singleplayer";
    private const string ELIMINATION_STATCODE = "unity-highestscore-elimination";
    private const string TEAMDEATHMATCH_STATCODE = "unity-highestscore-teamdeathmatch";

    private StatsEssentialsWrapper _statsWrapper;
    private string currentUserId;
    private string currentStatCode;

    // Start is called before the first frame update
    void Start()
    {
        _statsWrapper = TutorialModuleManager.Instance.GetModuleClass<StatsEssentialsWrapper>();

        GameManager.OnGameOver += CheckHighestScoreStats;
    }

    #region AB Functions Call

    private void CheckHighestScoreStats(GameModeEnum gameMode, InGameMode inGameMode, List<PlayerState> playerStates)
    {
        currentUserId = MultiRegistry.GetApiClient().session.UserId;

#if UNITY_SERVER
            if (inGameMode is InGameMode.OnlineEliminationGameMode or InGameMode.CreateMatchEliminationGameMode)
            {
                currentStatCode = ELIMINATION_STATCODE;
            }
            else if (inGameMode is InGameMode.OnlineDeathMatchGameMode or InGameMode.CreateMatchDeathMatchGameMode)
            {
                currentStatCode = TEAMDEATHMATCH_STATCODE;
            }

            Dictionary<string, float> userStats = playerStates.ToDictionary(state => state.playerId, state => state.score);
            _statsWrapper.BulkGetUsersStatFromServer(userStats.Keys.ToArray(), currentStatCode, result => OnBulkGetUserStatFromServer(result, userStats));
#endif

        if (gameMode is GameModeEnum.SinglePlayer)
        {
            currentStatCode = SINGLEPLAYER_STATCODE;
            PlayerState playerState = null;
            foreach (PlayerState currentPlayerState in playerStates)
            {
                if (currentPlayerState.playerId == currentUserId)
                {
                    playerState = currentPlayerState;
                    break;
                }
            }
            _statsWrapper.GetUserStatsFromClient(new string[] { currentStatCode }, null, result => OnGetUserStatsFromClient(result, playerState));
        }
    }

    #endregion

    #region Callback Functions

    private void OnBulkGetUserStatFromServer(Result<FetchUserStatistic> result, Dictionary<string, float> userStats)
    {
        if (!result.IsError)
        {
            // key: userId, value: stat value
            Dictionary<string, float> bulkUserStats = result.Value.UserStatistic.ToDictionary(stat => stat.UserId, stat => stat.Value);
            List<string> userIdToRemove = new List<string>();
            foreach (string userId in userStats.Keys)
            {
                if (bulkUserStats.ContainsKey(userId) && userStats[userId] < bulkUserStats[userId])
                {
                    userIdToRemove.Add(userId);
                }
            }
            foreach (var uId in userIdToRemove)
            {
                userStats.Remove(uId);
            }
            _statsWrapper.UpdateManyUserStatsFromServer(currentStatCode, userStats, OnUpdateStatsWithServerSdkCompleted);
        }
        else
        {
            _statsWrapper.UpdateManyUserStatsFromServer(currentStatCode, userStats, OnUpdateStatsWithServerSdkCompleted);
        }
    }

    private void OnGetUserStatsFromClient(Result<PagedStatItems> result, PlayerState playerState)
    {
        // if query success
        if (!result.IsError)
        {
            // key: statcode, value: stat value
            Dictionary<string, float> userStatItems = result.Value.data.ToDictionary(stat => stat.statCode, stat => stat.value);
            if (userStatItems.ContainsKey(currentStatCode) && playerState.score > userStatItems[currentStatCode])
            {
                _statsWrapper.UpdateUserStatsFromClient(currentStatCode, playerState.score, "", OnUpdateStatsWithClientSdkCompleted);
            }
            else if (!userStatItems.ContainsKey(currentStatCode))
            {
                _statsWrapper.UpdateUserStatsFromClient(currentStatCode, playerState.score, "", OnUpdateStatsWithClientSdkCompleted);
            }
        }
        else
        {
            // Create a new stat item since the stat doesn't exist yet
            _statsWrapper.UpdateUserStatsFromClient(currentStatCode, playerState.score, "", OnUpdateStatsWithClientSdkCompleted);
        }
    }

    private void OnUpdateStatsWithClientSdkCompleted(Result<UpdateUserStatItemValueResponse> result)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully update stat value with Client SDK!");
        }
    }

    private void OnUpdateStatsWithServerSdkCompleted(Result<StatItemOperationResult[]> result)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully update stats values with Server SDK!");
        }
    }

    #endregion
}
