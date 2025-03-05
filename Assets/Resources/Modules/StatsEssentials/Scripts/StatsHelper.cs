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

        GameManager.OnGameEnded += UpdateConnectedPlayersStatsOnGameEnds;
    }

    private void UpdateConnectedPlayersStatsOnGameEnds(GameManager.GameOverReason reason) 
    {
        if (reason != GameManager.GameOverReason.MatchEnded) 
        {
            return;
        }

        GameModeEnum gameMode = GameManager.Instance.GameMode;
        InGameMode inGameMode = GameManager.Instance.InGameMode;
        List<PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates.Values.ToList();

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
                case InGameMode.MatchmakingElimination:
                case InGameMode.CreateMatchElimination:
                    targetStatCode = EliminationStatCode;
                    break;
                case InGameMode.MatchmakingTeamDeathmatch:
                case InGameMode.CreateMatchTeamDeathmatch:
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

        Dictionary<string, float> userStats = playerStates.ToDictionary(state => state.PlayerId, state => state.Score);
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
        float highestLocalScore = playerStates.Count > 0 ? playerStates.OrderByDescending(p => p.Score).ToArray()[0].Score : 0.0f;
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
