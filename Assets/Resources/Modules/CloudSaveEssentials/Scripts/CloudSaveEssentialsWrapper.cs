// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class CloudSaveEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private CloudSave cloudSave;

    // Start is called before the first frame update
    void Start()
    {
        cloudSave = AccelByteSDK.GetClientRegistry().GetApi().GetCloudSave();
    }

    #region AB Service Functions

    public void SaveUserRecord(string recordKey, Dictionary<string, object> recordRequest, ResultCallback resultCallback)
    {
        cloudSave.SaveUserRecord(
            recordKey, 
            recordRequest, 
            result => OnSaveUserRecordCompleted(result, resultCallback),
            true
        );
    }

    public void GetUserRecord(string recordKey, ResultCallback<UserRecord> resultCallback)
    {
        cloudSave.GetUserRecord(
            recordKey,
            result => OnGetUserRecordCompleted(result, resultCallback)
        );
    }

    public void DeleteUserRecord(string recordKey, ResultCallback resultCallback)
    {
        cloudSave.DeleteUserRecord(
            recordKey,
            result => OnDeleteUserRecordCompleted(result, resultCallback)
        );
    }
    
    #endregion

    #region Callback Functions

    private void OnSaveUserRecordCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Save Player Record from Client successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Save Player Record from Client failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    private void OnGetUserRecordCompleted(Result<UserRecord> result, ResultCallback<UserRecord> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Get Player Record from Client successful.");
        }
        else
        {
            BytewarsLogger.LogWarning($"Get Player Record from Client failed. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
        private void OnDeleteUserRecordCompleted(Result result, ResultCallback customCallback = null)
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Delete Player Record from Client successful.");
            }
            else
            {
                BytewarsLogger.LogWarning($"Delete Player Record from Client failed. Message: {result.Error.Message}");
            }
            
            customCallback?.Invoke(result);
        }

    #endregion
}
