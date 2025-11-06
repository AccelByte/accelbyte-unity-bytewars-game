// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System;
using System.Linq;

public class PeriodicLeaderboardWrapper : MonoBehaviour
{
    // AGS Game SDK references
    private Leaderboard leaderboard;
    private Statistic statistic;
    
    private void Start()
    {
        leaderboard = AccelByteSDK.GetClientRegistry().GetApi().GetLeaderboard();
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
    }

    #region AB Service Functions

    public void GetStatCycleConfigByName(string cycleName, ResultCallback<StatCycleConfig> resultCallback)
    {
        statistic.GetListStatCycleConfigs(result => OnGetStatCycleConfigByNameCompleted(cycleName, result, resultCallback));
    }
    
    public void GetRankingsByCycle(string leaderboardCode, string cycleId, ResultCallback<LeaderboardRankingResult> resultCallback, int offset = default, int limit = default)
    {
        leaderboard.GetRankingsByCycle(
            leaderboardCode, 
            cycleId, 
            result => OnGetRankingsByCycleCompleted(result, resultCallback),
            offset,
            limit
        );
    }

    public void GetUserRanking(string userId, string leaderboardCode, ResultCallback<UserRankingDataV3> resultCallback)
    {
        leaderboard.GetUserRankingV3(
            userId,
            leaderboardCode,
            result => OnGetUserRankingCompleted(result, resultCallback)
        );
    }

    #endregion

    #region Callback Functions

    private void OnGetStatCycleConfigByNameCompleted(string cycleName, Result<PagedStatCycleConfigs> result, ResultCallback<StatCycleConfig> customCallback)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Get stat cycle config by name failed. Message: {result.Error.Message}");
            customCallback.Invoke(Result<StatCycleConfig>.CreateError(result.Error));
            return;
        }

        StatCycleConfig statCycle = result.Value.Data.FirstOrDefault(x => x.Name.Equals(cycleName, StringComparison.OrdinalIgnoreCase));
        if (statCycle == null)
        {
            string errorMessage = $"Stat cycle config with cycle name {cycleName} not found.";
            BytewarsLogger.LogWarning($"Get stat cycle config by name failed. Message: {errorMessage}");
            customCallback.Invoke(Result<StatCycleConfig>.CreateError(ErrorCode.NotFound, errorMessage));
            return;
        }

        BytewarsLogger.Log("Get stat cycle config by name success!");
        customCallback.Invoke(Result<StatCycleConfig>.CreateOk(statCycle));
    }

    private void OnGetRankingsByCycleCompleted(Result<LeaderboardRankingResult> result, ResultCallback<LeaderboardRankingResult> customCallback)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Get Ranking by Cycle success!");
        }
        else
        {
            BytewarsLogger.LogWarning($"Get Rankings by Cycle failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    private void OnGetUserRankingCompleted(Result<UserRankingDataV3> result, ResultCallback<UserRankingDataV3> customCallback)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Get User Ranking V3 successfull.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Get User Ranking V3 failed. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    #endregion
}
