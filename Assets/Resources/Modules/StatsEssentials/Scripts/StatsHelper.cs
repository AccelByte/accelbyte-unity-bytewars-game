// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class StatsHelper : MonoBehaviour
{
    // statcodes' name configured in Admin Portal
    private const string SinglePlayerStatCode = "unity-highestscore-singleplayer";
    private const string EliminationStatCode = "unity-highestscore-elimination";
    private const string TeamDeathmatchStatCode = "unity-highestscore-teamdeathmatch";

    private StatsEssentialsWrapper statsWrapper;

    // Start is called before the first frame update
    void Start()
    {
        statsWrapper = TutorialModuleManager.Instance.GetModuleClass<StatsEssentialsWrapper>();

        GameManager.OnGameOver += UpdateConnectedPlayersStatsOnGameEnds;
    }

    private void UpdateConnectedPlayersStatsOnGameEnds(GameModeEnum gameMode, InGameMode inGameMode, List<PlayerState> playerStates) 
    {
        // Set the correct statistic code based on game mode.
        string targetStatCode = string.Empty;
        if (gameMode is GameModeEnum.SinglePlayer or GameModeEnum.LocalMultiplayer) 
        {
            targetStatCode = SinglePlayerStatCode;
        }
        else if (gameMode is GameModeEnum.OnlineMultiplayer) 
        {
            switch (inGameMode)
            {
                case InGameMode.OnlineEliminationGameMode:
                case InGameMode.CreateMatchEliminationGameMode:
                    targetStatCode = EliminationStatCode;
                    break;
                case InGameMode.OnlineDeathMatchGameMode:
                case InGameMode.CreateMatchDeathMatchGameMode:
                    targetStatCode = TeamDeathmatchStatCode;
                    break;
            }
        }

        if (string.IsNullOrEmpty(targetStatCode)) 
        {
            BytewarsLogger.LogWarning($"Failed to update the stats of connected players when the game ended. Target stat code to update is empty.");
            return;
        }

#if UNITY_SERVER
        BytewarsLogger.Log($"[Server] Update the stats of connected players when the game ended. Game mode: {gameMode}. In game mode: {inGameMode}");

        Dictionary<string, float> userStats = playerStates.ToDictionary(state => state.playerId, state => state.score);
        List<UserStatItemUpdate> statItems = new List<UserStatItemUpdate>();
        foreach (KeyValuePair<string, float> userStat in userStats)
        {
            UserStatItemUpdate statItem = new UserStatItemUpdate()
            {
                updateStrategy = StatisticUpdateStrategy.MAX,
                statCode = targetStatCode,
                userId = userStat.Key,
                value = userStat.Value
            };
            statItems.Add(statItem);
        }

        statsWrapper.UpdateManyUserStatsFromServer(targetStatCode, statItems, (Result<StatItemOperationResult[]> result) =>
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
        string targetUserID = AccelByteSDK.GetClientRegistry().GetApi().session.UserId;
        if (string.IsNullOrEmpty(targetUserID))
        {
            BytewarsLogger.LogWarning($"[Client] Failed to update the stats of connected players when the game ended. Current logged-in player user ID is not found.");
            return;
        }

        /* Local gameplay only has one valid account, which is the player who logged in to the game.
         * Thus, set the stats based on the highest data.*/
        float highestLocalScore = playerStates.Count > 0 ? playerStates.OrderByDescending(p => p.score).ToArray()[0].score : 0.0f;
        PublicUpdateUserStatItem statItem = new PublicUpdateUserStatItem
        {
            updateStrategy = StatisticUpdateStrategy.MAX,
            value = highestLocalScore
        };
        statsWrapper.UpdateUserStatsFromClient(targetStatCode, statItem, string.Empty, (Result<UpdateUserStatItemValueResponse> result) =>
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
}
