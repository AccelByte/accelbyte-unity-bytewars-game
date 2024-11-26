// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class StatsEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private Statistic statistic;
    private ServerStatistic serverStatistic;
    
    // Start is called before the first frame update
    void Start()
    {
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
        serverStatistic = AccelByteSDK.GetServerRegistry().GetApi().GetStatistic();
    }

    #region AB Service Functions

    /// <summary>
    /// Update User Statistics value from Client side
    /// </summary>
    /// <param name="statCode">stat code of the desired stat items</param>
    /// <param name="statItem">stat item containing desired new stat value</param>
    /// <param name="additionalKey">additional custom key that will be added to the slot</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void UpdateUserStatsFromClient(string statCode, PublicUpdateUserStatItem statItem, string additionalKey, ResultCallback<UpdateUserStatItemValueResponse> resultCallback = null)
    {        
        statistic.UpdateUserStatItemsValue(
            statCode,
            additionalKey,
            statItem,
            result => OnUpdateUserStatsFromClientCompleted(result, resultCallback)
        );
    }

    /// <summary>
    /// Update User Statistics value from Server side
    /// </summary>
    /// <param name="statCode">stat code of the desired stat item</param>
    /// <param name="statItems">a list of stat item containing desired new stat value and the target user ID</param>
    /// /// <param name="resultCallback">callback function to get result from other script</param>
    public void UpdateManyUserStatsFromServer(string statCode, List<UserStatItemUpdate> statItems, ResultCallback<StatItemOperationResult[]> resultCallback)
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
    private void OnUpdateUserStatsFromClientCompleted(Result<UpdateUserStatItemValueResponse> result, ResultCallback<UpdateUserStatItemValueResponse> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Update User's Stat Items from Client successful.");
        }
        else
        {
            Debug.Log($"Update User's Stat Items from Client failed. Message: {result.Error.Message}");
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
            Debug.Log("Update User's Stat Items from Server successful.");
        }
        else
        {
            Debug.Log($"Update User's Stat Items from Server failed. Message: {result.Error.Message}");
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
            Debug.Log("Get User's Stat Items from Client successful.");
        }
        else
        {
            Debug.Log($"Get User's Stat Items from Client failed. Message: {result.Error.Message}");
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
            Debug.Log("Get User's Stat Items from Server successful.");
        }
        else
        {
            Debug.Log($"Get User's Stat Items from Server failed. Message: {result.Error.Message}");
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
            Debug.Log("Reset User Stat Item's value from Client successful.");
        }
        else
        {
            Debug.Log($"Reset User Stat Item's value from Client failed. Message: {result.Error.Message}");
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
            Debug.Log("Reset User Stat Item's value from Server successful.");
        }
        else
        {
            Debug.Log($"Reset User Stat Item's value from Server failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    #endregion
}
