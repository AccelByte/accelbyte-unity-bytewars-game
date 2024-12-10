using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;

public class PeriodicLeaderboardEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private Leaderboard leaderboard;
    private Statistic statistic;
    
    // Start is called before the first frame update
    void Start()
    {
        leaderboard = AccelByteSDK.GetClientRegistry().GetApi().GetLeaderboard();
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
    }

    #region AB Service Functions

    public void GetStatCycleConfig(string cycleId, ResultCallback<StatCycleConfig> resultCallback)
    {
        statistic.GetStatCycleConfig(
            cycleId, 
            result => OnGetStatCycleConfigCompleted(result, resultCallback) 
        );
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

    #endregion

    #region Callback Functions

    private void OnGetStatCycleConfigCompleted(Result<StatCycleConfig> result, ResultCallback<StatCycleConfig> customCallback)
    {
        if (!result.IsError)
        {
            Debug.Log("Get Stat's Cycle Config info success!");
        }
        else
        {
            Debug.Log($"Get Stat's Cycle Config info failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    private void OnGetRankingsByCycleCompleted(Result<LeaderboardRankingResult> result, ResultCallback<LeaderboardRankingResult> customCallback)
    {
        if (!result.IsError)
        {
            Debug.Log("Get Ranking by Cycle success!");
        }
        else
        {
            Debug.Log($"Get Rankings by Cycle failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    #endregion
}
