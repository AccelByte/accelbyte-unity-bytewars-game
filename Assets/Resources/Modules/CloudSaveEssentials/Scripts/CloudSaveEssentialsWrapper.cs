// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class CloudSaveEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private CloudSave cloudSave;
    private Lobby lobby;

    void Start()
    {
        cloudSave = AccelByteSDK.GetClientRegistry().GetApi().GetCloudSave();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        lobby.Connected += LoadGameOptions;
        OptionsMenu.OnOptionsMenuActivated += (musicVolume, sfxVolume) => LoadGameOptions();
        OptionsMenu.OnOptionsMenuDeactivated += SaveGameOptions;
    }

    #region Helper Functions

    public void LoadGameOptions()
    {
        GetUserRecord(CloudSaveEssentialsModels.GameOptionsRecordKey, (Result<UserRecord> result) => 
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to load game options from Cloud Save. Error {result.Error.Code}: {result.Error.Message}");
                return;
            }

            BytewarsLogger.Log($"Success to load game options from Cloud Save.");

            // Apply record data to local game option settings.
            Dictionary<string, object> recordData = result.Value.value;
            if (recordData != null)
            {
                float musicVolume = Convert.ToSingle(recordData[CloudSaveEssentialsModels.MusicVolumeItemName]);
                float sfxVolume = Convert.ToSingle(recordData[CloudSaveEssentialsModels.SfxVolumeItemName]);
                AudioManager.Instance.SetMusicVolume(musicVolume);
                AudioManager.Instance.SetSfxVolume(sfxVolume);
            }
        });
    }

    private void SaveGameOptions(float musicVolume, float sfxVolume)
    {
        // Collect the local game options values to save.
        Dictionary<string, object> gameOptions = new Dictionary<string, object>()
        {
            {
                CloudSaveEssentialsModels.MusicVolumeItemName, 
                AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.MusicAudio)
            },
            {
                CloudSaveEssentialsModels.SfxVolumeItemName, 
                AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio)
            }
        };

        SaveUserRecord(CloudSaveEssentialsModels.GameOptionsRecordKey, gameOptions, (Result result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to save game options to Cloud Save. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to save game options to Cloud Save.");
            }
        });
    }

    #endregion

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
