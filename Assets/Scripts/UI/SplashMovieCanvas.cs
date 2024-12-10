// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine.Video;
using UnityEngine;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;

public static class SplashMovieCanvas
{
    private const float SplashMovieDuration = 4f;
    private const string SplashCanvasName = "Splash Canvas";
    private const string SplashMoviePath = "/Movies/AccelByte_IntroDark_720.mp4";

    public static async void PlaySplashMovie(Action onComplete = null)
    {
        GlobalVolumeUtils.GlobalVolumeObject.SetActive(false);
        GameObject backgroundObject = GameObject.Find("Background");
        if (backgroundObject != null)
        {
            backgroundObject.SetActive(false);
        }

        GameObject splashCanvas = CreateSplashMovieCanvas();
        VideoPlayer videoPlayer = AddVideoPlayerComponent(splashCanvas);
        videoPlayer.Play();

        await UniTask.Delay(TimeSpan.FromSeconds(SplashMovieDuration));
        Object.Destroy(splashCanvas);

        GlobalVolumeUtils.GlobalVolumeObject.SetActive(true);
        if (backgroundObject != null)
        {
            backgroundObject.SetActive(true);
        }

        onComplete?.Invoke();
    }

    private static GameObject CreateSplashMovieCanvas()
    {
        GameObject splashCanvas = new(SplashCanvasName);
        splashCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        return splashCanvas;
    }

    private static VideoPlayer AddVideoPlayerComponent(GameObject splashCanvas)
    {
        VideoPlayer videoPlayer = splashCanvas.AddComponent<VideoPlayer>();
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.url = Application.streamingAssetsPath + SplashMoviePath;
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.targetCamera = GameManager.Instance != null ? GameManager.Instance.MainCamera : Camera.main;

        return videoPlayer;
    }
}
