// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class StatsEssentialsWrapper : MonoBehaviour
{
    // AGS Game SDK references
    private Statistic statistic;
    private ServerStatistic serverStatistic;
    
    void Start()
    {
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
#if UNITY_SERVER
        serverStatistic = AccelByteSDK.GetServerRegistry().GetApi().GetStatistic();
#endif

        GameManager.OnGameEnded += UpdateConnectedPlayersStatsOnGameEnds;
    }

    #region AB Service Functions

    /// <summary>
    /// Update User Statistics value from Client side
    /// </summary>
    /// <param name="statCode">stat code of the desired stat items</param>
    /// <param name="statItem">stat item containing desired new stat value</param>
    /// <param name="additionalKey">additional custom key that will be added to the slot</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void UpdateUserStatsFromClient(List<StatItemUpdate> statItems, ResultCallback<StatItemOperationResult[]> resultCallback = null)
    {
        statistic.UpdateUserStatItems(
            statItems.ToArray(),
            result => OnUpdateUserStatsFromClientCompleted(result, resultCallback)
        );
    }

    /// <summary>
    /// Update User Statistics value from Server side
    /// </summary>
    /// <param name="statCode">stat code of the desired stat item</param>
    /// <param name="statItems">a list of stat item containing desired new stat value and the target user ID</param>
    /// /// <param name="resultCallback">callback function to get result from other script</param>
    public void UpdateManyUserStatsFromServer(List<UserStatItemUpdate> statItems, ResultCallback<StatItemOperationResult[]> resultCallback)
    {   
        serverStatistic.UpdateManyUsersStatItems(
            statItems.ToArray(),
            result => OnUpdateManyUserStatsFromServerCompleted(result, resultCallback));
    }
    
    /// <summary>
    /// Get User Statistics from Client side
    /// </summary>
    /// <param name="statCodes">list of stat codes of the desired stat items</param>
    /// <param name="tags">list of custom tags of the desired stat items</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void GetUserStatsFromClient(string[] statCodes, string[] tags, ResultCallback<PagedStatItems> resultCallback)
    {
        statistic.GetUserStatItems(
            statCodes, 
            tags, 
            result => OnGetUserStatsFromClientCompleted(result, resultCallback)
        );
    }

    /// <summary>
    /// Get Multiple Users Statistics in bulk from Server side
    /// </summary>
    /// <param name="userId">list of user id of the desired user's stats</param>
    /// <param name="statCodes">stat code name of the desired stat item</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void BulkGetUsersStatFromServer(string[] userIds, string statCode, ResultCallback<FetchUserStatistic> resultCallback)
    {
        serverStatistic.BulkFetchStatItemsValue(
            statCode,
            userIds,
            result => OnGetUserStatItemsFromServerCompleted(result, resultCallback)
        );
    }
    
    /// <summary>
    /// Reset User Stat Item's value from Client side
    /// </summary>
    /// <param name="statCode">stat code of the desired stat items</param>
    /// <param name="additionalKey">additional custom key that will be added to the slot</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void ResetUserStatsFromClient(string statCode, string additionalKey, ResultCallback<UpdateUserStatItemValueResponse> resultCallback = null)
    {
        PublicUpdateUserStatItem userStatItem = new PublicUpdateUserStatItem
        {
            updateStrategy = StatisticUpdateStrategy.OVERRIDE,
            value = 0
        };
        
        statistic.UpdateUserStatItemsValue(
            statCode,
            additionalKey,
            userStatItem,
            result => OnResetUserStatsFromClientCompleted(result, resultCallback)
        );
    }
    
    /// <summary>
    /// Reset User Stat Items values from Server side
    /// </summary>
    /// <param name="userId">user id of the desired user's stats</param>
    /// <param name="statCodes">list of desired stat code</param>
    /// <param name="additionalKey">additional custom key that will be added to the slot</param>
    /// /// <param name="resultCallback">callback function to get result from other script</param>
    public void ResetUserStatsFromServer(string userId, string[] statCodes, string additionalKey, ResultCallback<StatItemOperationResult[]> resultCallback)
    {
        List<StatItemUpdate> bulkUpdateUserStatItems = new List<StatItemUpdate>();
        foreach (string statCode in statCodes)
        {
            StatItemUpdate userStatItem = new StatItemUpdate
            {
                statCode = statCode,
                updateStrategy = StatisticUpdateStrategy.OVERRIDE,
                value = 0
            };
            
            bulkUpdateUserStatItems.Add(userStatItem);
        }
        
        serverStatistic.UpdateUserStatItems(
            userId,
            bulkUpdateUserStatItems.ToArray(),
            result => OnResetUserStatsFromServerCompleted(result, resultCallback)
        );
    }

    #endregion

    #region Callback Functions

    /// <summary>
    /// Default Callback for Statistic's UpdateUserStatItems() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnUpdateUserStatsFromClientCompleted(Result<StatItemOperationResult[]> result, ResultCallback<StatItemOperationResult[]> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Update User's Stat Items from Client successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Update User's Stat Items from Client failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    /// <summary>
    /// Default Callback for ServerStatistic's UpdateUserStatItems() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnUpdateManyUserStatsFromServerCompleted(Result<StatItemOperationResult[]> result, ResultCallback<StatItemOperationResult[]> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Update User's Stat Items from Server successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Update User's Stat Items from Server failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    /// <summary>
    /// Default Callback for Statistic's GetUserStatItems() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnGetUserStatsFromClientCompleted(Result<PagedStatItems> result, ResultCallback<PagedStatItems> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Get User's Stat Items from Client successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Get User's Stat Items from Client failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    /// <summary>
    /// Default Callback for ServerStatistic's GetUserStatItems() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnGetUserStatItemsFromServerCompleted(Result<FetchUserStatistic> result, ResultCallback<FetchUserStatistic> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Get User's Stat Items from Server successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Get User's Stat Items from Server failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    /// <summary>
    /// Default Callback for Reset Stat Items value from Client
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnResetUserStatsFromClientCompleted(Result<UpdateUserStatItemValueResponse> result, ResultCallback<UpdateUserStatItemValueResponse> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Reset User Stat Item's value from Client successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Reset User Stat Item's value from Client failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    /// <summary>
    /// Default Callback for Reset Stat Items values from Server
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnResetUserStatsFromServerCompleted(Result<StatItemOperationResult[]> result, ResultCallback<StatItemOperationResult[]> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Reset User Stat Item's value from Server successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Reset User Stat Item's value from Server failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    #endregion

    #region Helper Functions
    private void UpdateConnectedPlayersStatsOnGameEnds(GameManager.GameOverReason reason)
    {
        if (reason != GameManager.GameOverReason.MatchEnded)
        {
            return;
        }

        GameModeEnum gameMode = GameManager.Instance.GameMode;
        InGameMode inGameMode = GameManager.Instance.InGameMode;
        StatsEssentialsModels.GameStatsData gameStatsData = StatsEssentialsModels.GetGameStatsDataByGameMode(inGameMode);
        List<PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates.Values.ToList();

        // Store statistics to update.
        List<UserStatItemUpdate> statItems = new();
        Dictionary<int, (bool isWinner, int score, int kills, int deaths)> teamStats = new();
        GameManager.Instance.GetWinner(out TeamState winnerTeam, out PlayerState winnerPlayer);
        foreach (PlayerState playerState in playerStates)
        {
            if (!teamStats.ContainsKey(playerState.TeamIndex))
            {
                List<PlayerState> teamPlayers = playerStates.Where(p => p.TeamIndex == playerState.TeamIndex).ToList();
                teamStats.Add(playerState.TeamIndex, new()
                {
                    isWinner = winnerTeam != null ? playerState.TeamIndex == winnerTeam.teamIndex : false,
                    score = (int)teamPlayers.Sum(p => p.Score),
                    kills = teamPlayers.Sum(p => p.KillCount),
                    deaths = (GameData.GameModeSo.PlayerStartLives * teamPlayers.Count) - teamPlayers.Sum(p => p.Lives)
                });
            }

            (bool isWinner, int score, int kills, int deaths) = teamStats[playerState.TeamIndex];

            // Highest score statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.MAX,
                statCode = gameStatsData.HighestScoreStats.StatCode,
                userId = playerState.PlayerId,
                value = score
            });

            // Total score statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.INCREMENT,
                statCode = gameStatsData.TotalScoreStats.StatCode,
                userId = playerState.PlayerId,
                value = score
            });

            // Matches played statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.INCREMENT,
                statCode = gameStatsData.MatchesPlayedStats.StatCode,
                userId = playerState.PlayerId,
                value = 1
            });

            // Matches won statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.INCREMENT,
                statCode = gameStatsData.MatchesWonStats.StatCode,
                userId = playerState.PlayerId,
                value = isWinner ? 1 : 0
            });

            // Kill count statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.INCREMENT,
                statCode = gameStatsData.KillCountStats.StatCode,
                userId = playerState.PlayerId,
                value = kills
            });

            // Death statistic
            statItems.Add(new()
            {
                updateStrategy = StatisticUpdateStrategy.INCREMENT,
                statCode = gameStatsData.DeathStats.StatCode,
                userId = playerState.PlayerId,
                value = deaths
            });
        }

#if UNITY_SERVER
        BytewarsLogger.Log($"[Server] Update the stats of connected players when the game ended. Game mode: {gameMode}. In game mode: {inGameMode}");
        UpdateManyUserStatsFromServer(statItems, (Result<StatItemOperationResult[]> result) =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("[Server] Successfully updated the stats of connected players when the game ended.");
            }
            else
            {
                BytewarsLogger.LogWarning($"[Server] Failed to update the stats of connected players when the game ended. Error: {result.Error.Message}");
            }
        });
#else
        BytewarsLogger.Log($"[Client] Update the stats of local connected players when the game ended. Game mode: {gameMode}. In game mode: {inGameMode}");

        /* Local gameplay only has one valid account, which is the player who logged in to the game.
         * Thus, we can only update the stats based on that player's user ID. */
        List<StatItemUpdate> localPlayerStatItems = statItems
            .Where(x => x.userId == GameData.CachedPlayerState.PlayerId)
            .Select(x => new StatItemUpdate
            {
                updateStrategy = x.updateStrategy,
                value = x.value,
                statCode = x.statCode,
                additionalData = x.additionalData
            }).ToList();

        UpdateUserStatsFromClient(localPlayerStatItems, (Result<StatItemOperationResult[]> result) =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("[Client] Successfully updated the stats of connected players when the game ended.");
            }
            else
            {
                BytewarsLogger.LogWarning($"[Client] Failed to update the stats of connected players when the game ended. Error: {result.Error.Message}");
            }
        });
#endif
    }
    #endregion
}
