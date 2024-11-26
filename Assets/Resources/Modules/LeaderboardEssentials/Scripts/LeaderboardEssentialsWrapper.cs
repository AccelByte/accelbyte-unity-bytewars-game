// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class LeaderboardEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private Leaderboard leaderboard;
    
    // Start is called before the first frame update
    void Start()
    {
        leaderboard = AccelByteSDK.GetClientRegistry().GetApi().GetLeaderboard();
    }

    #region AB Service Functions

    /// <summary>
    /// Get list of available leaderboards configured in Admin Portal
    /// </summary>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void GetLeaderboardList(ResultCallback<LeaderboardPagedListV3> resultCallback)
    {
        leaderboard.GetLeaderboardListV3(
            result => OnGetLeaderboardListCompleted(result, resultCallback)
        );
    }
    
    /// <summary>
    /// Get rankings list of the desired leaderboard
    /// </summary>
    /// <param name="leaderboardCode">leaderboard code of the desired leaderboard</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void GetRankings(string leaderboardCode, ResultCallback<LeaderboardRankingResult> resultCallback, int offset = default, int limit = default)
    {
        BytewarsLogger.Log($"Get leaderboard {leaderboardCode} rankings.");

        leaderboard.GetRankingsV3(
            leaderboardCode,
            result => OnGetRankingsCompleted(result, resultCallback), 
            offset, 
            limit
        );
    }

    public void GetUserRanking(string userId, string leaderboardCode, ResultCallback<UserRankingDataV3> resultCallback)
    {
        BytewarsLogger.Log($"Get user {userId} ranking in leaderboard {leaderboardCode} rankings.");

        leaderboard.GetUserRankingV3(
            userId,
            leaderboardCode,
            result => OnGetUserRankingCompleted(result, resultCallback)
        );
    }
    
    #endregion

    #region Callback Functions

    /// <summary>
    /// Default Callback for Leaderboard V3's GetLeaderboardListV3() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnGetLeaderboardListCompleted(Result<LeaderboardPagedListV3> result, ResultCallback<LeaderboardPagedListV3> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Get Leaderboard List V3 successful.");
        }
        else
        {
            Debug.Log($"Get Leaderboard List V3 failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    /// <summary>
    /// Default Callback for Leaderboard V3's GetRankingsV3() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnGetRankingsCompleted(Result<LeaderboardRankingResult> result, ResultCallback<LeaderboardRankingResult> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Get Rankings V3 successful.");
        }
        else
        {
            Debug.Log($"Get Rankings V3 failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    /// <summary>
    /// Default Callback for Leaderboard V3's GetUserRankingV3() function
    /// </summary>
    /// <param name="result">result of the GetUserStatItems() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnGetUserRankingCompleted(Result<UserRankingDataV3> result, ResultCallback<UserRankingDataV3> customCallback)
    {
        if (!result.IsError)
        {
            Debug.Log("Get User Ranking V3 successfull.");
        }
        else
        {
            Debug.Log($"Get User Ranking V3 failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    #endregion
}
