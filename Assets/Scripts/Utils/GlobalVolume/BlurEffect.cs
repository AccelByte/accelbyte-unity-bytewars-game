// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BlurEffect
{
    private const string BlurProfileName = "BlurProfile";
    private const string ResourcePath = "Settings/" + BlurProfileName;
    
    private static Volume _cachedBlurProfileVolume;

    #region Lifecycle methods

    static BlurEffect()
    {
        _cachedBlurProfileVolume = GetBlurProfileByGlobalVolume(GlobalVolumeUtils.GlobalVolumeObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cachedBlurProfileVolume = GetBlurProfileByGlobalVolume(GlobalVolumeUtils.GlobalVolumeObject);
    }

    #endregion Lifecycle methods

    #region Global Volume methods

    private static Volume GetBlurProfileByGlobalVolume(GameObject globalVolumeObject, string blurProfileName = BlurProfileName)
    {
        if (_cachedBlurProfileVolume != null)
        {
            return _cachedBlurProfileVolume;
        }

        if (globalVolumeObject == null)
        {
            globalVolumeObject = GlobalVolumeUtils.GlobalVolumeObject;
        }

        if (globalVolumeObject == null)
        {
            globalVolumeObject = GlobalVolumeUtils.GetGlobalVolumeInScene();
        }

        Volume[] volumes = globalVolumeObject.GetComponentsInChildren<Volume>();
        _cachedBlurProfileVolume = volumes.FirstOrDefault(volume => volume.profile.name.Equals(blurProfileName));
        return _cachedBlurProfileVolume == null ? AddBlurProfileToGlobalVolume(globalVolumeObject) : _cachedBlurProfileVolume;
    }

    private static Volume AddBlurProfileToGlobalVolume(GameObject globalVolumeObject)
    {
        Volume blurProfileVolume = globalVolumeObject.AddComponent<Volume>();
        blurProfileVolume.isGlobal = true;
        
        VolumeProfile blurProfile = Resources.Load<VolumeProfile>(ResourcePath);
        blurProfileVolume.profile = blurProfile;
        
        _cachedBlurProfileVolume = blurProfileVolume;

        return blurProfileVolume;
    }

    #endregion Global Volume methods

    #region Blur Effect methods

    public static async void ApplyBlurEffect(float targetBlurAmount = 0.2f, float transitionDuration = 0.2f,
        Action onComplete = null)
    {
        Volume blurProfileVolume = GetBlurProfileByGlobalVolume(null);
        await TransitionBlurEffect(blurProfileVolume, targetBlurAmount, transitionDuration);
        onComplete?.Invoke();
    }

    public static async void RemoveBlurEffect(float transitionDuration = 0.2f, Action onComplete = null)
    {
        Volume blurProfileVolume = GetBlurProfileByGlobalVolume(null);
        await TransitionBlurEffect(blurProfileVolume, 0f, transitionDuration);
        onComplete?.Invoke();
    }

    private static async Task TransitionBlurEffect(Volume blurProfileVolume, float targetBlur, float duration)
    {
        if (blurProfileVolume == null)
        {
            return;
        }

        float initialBlur = blurProfileVolume.weight;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float ratio = elapsedTime / duration;
            blurProfileVolume.weight = Mathf.Lerp(initialBlur, targetBlur, ratio);

            await Task.Yield();
        }
        
        blurProfileVolume.weight = targetBlur;
    }

    #endregion Blur Effect methods
}
