// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlobalVolumeUtils
{
    public static GameObject GlobalVolumeObject { get; private set; }

    private const string GlobalVolumeName = "Global Volume";

    #region Lifecycle methods

    static GlobalVolumeUtils()
    {
        GlobalVolumeObject = GetGlobalVolumeInScene();
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GlobalVolumeObject = GetGlobalVolumeInScene();
    }

    #endregion Lifecycle methods

    #region Global Volume methods

    public static GameObject GetGlobalVolumeInScene()
    {
        GlobalVolumeObject = GameObject.Find(GlobalVolumeName);
        return GlobalVolumeObject == null ? CreateGlobalVolume() : GlobalVolumeObject;
    }

    private static GameObject CreateGlobalVolume()
    {
        GameObject globalVolumeObject = new(GlobalVolumeName);

        GlobalVolumeObject = globalVolumeObject;
        return globalVolumeObject;
    }

    #endregion Global Volume methods
}
