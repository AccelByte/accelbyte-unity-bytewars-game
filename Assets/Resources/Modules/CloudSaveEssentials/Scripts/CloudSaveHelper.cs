// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class CloudSaveHelper : MonoBehaviour
{
    // Player record key and configurations
    private const string GameOptionsRecordKey = "GameOptions-Sound";
    private const string MusicVolumeItemName = "musicvolume";
    private const string SfxVolumeItemName = "sfxvolume";

    private CloudSaveEssentialsWrapper cloudSaveWrapper;
    private Dictionary<string, object> volumeSettings;

    // Start is called before the first frame update
    void Start()
    {
        // Get cloud save's wrapper
        cloudSaveWrapper = TutorialModuleManager.Instance.GetModuleClass<CloudSaveEssentialsWrapper>();

        // Initialize dictionary with volume values stored in PlayerPrefs
        volumeSettings = new Dictionary<string, object>()
        {
            {MusicVolumeItemName, AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.MusicAudio)},
            {SfxVolumeItemName, AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio)}
        };

        LoginHandler.OnLoginComplete += tokenData => GetGameOptions();
        OptionsMenu.OnOptionsMenuActivated += (musicVolume, sfxVolume) => GetGameOptions();
        OptionsMenu.OnOptionsMenuDeactivated += (musicVolume, sfxVolume) => UpdateGameOptions(musicVolume, sfxVolume);
    }

    #region Game Options Getter Setter Function

    public void GetGameOptions()
    {
        cloudSaveWrapper.GetUserRecord(GameOptionsRecordKey, OnGetGameOptionsCompleted);
    }

    private void SaveGameOptions()
    {
        cloudSaveWrapper.SaveUserRecord(GameOptionsRecordKey, volumeSettings, OnSaveGameOptionsCompleted);
    }

    private void UpdateGameOptions(float musicVolume, float sfxVolume)
    {
        volumeSettings[MusicVolumeItemName] = musicVolume;
        volumeSettings[SfxVolumeItemName] = sfxVolume;
        
        SaveGameOptions();
    }

    #endregion

    #region Callback Function

    private void OnGetGameOptionsCompleted(Result<UserRecord> result)
    {
        if (!result.IsError)
        {
            Dictionary<string, object> recordData = result.Value.value;
            if (recordData != null)
            {
                volumeSettings[MusicVolumeItemName] = recordData[MusicVolumeItemName];
                AudioManager.Instance.SetMusicVolume(Convert.ToSingle(recordData[MusicVolumeItemName]));

                volumeSettings[SfxVolumeItemName] = recordData[SfxVolumeItemName];
                AudioManager.Instance.SetSfxVolume(Convert.ToSingle(recordData[SfxVolumeItemName]));
            }
        }
        else
        {
            SaveGameOptions();
        }
    }

    private void OnSaveGameOptionsCompleted(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Player Settings updated to cloud save!");
        }
    }

    #endregion
}
