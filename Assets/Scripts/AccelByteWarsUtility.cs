// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Globalization;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;

public class AccelByteWarsUtility
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void JSCopyToClipboard(string text);
#endif

    public static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            BytewarsLogger.LogWarning("Attempted to copy an empty string.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        JSCopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
        BytewarsLogger.Log($"Copied to clipboard: {text}");
    }

    public static string GetNowDate()
    {
        return DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.F", CultureInfo.InvariantCulture);
    }

    public static string GetDefaultDisplayNameByUserId(string userId) 
    {
        // Return first five digit of the user id with "player" prefix.
        return $"Player-{(string.IsNullOrEmpty(userId) ? "Unknown" : userId[..5])}";
    }

    public static string GenerateObjectEntityId(GameObject gameObject) 
    {
        return $"{gameObject.name}_{Mathf.Abs(gameObject.GetInstanceID())}";
    }

    public static async UniTask StartCountdown(
        float countdownInSeconds,
        Action<float /*remainingTime*/, bool /*isComplete*/> callback,
        CancellationToken cancelToken = default)
    {
        float remaining = countdownInSeconds;

        while (remaining > 0f)
        {
            if (cancelToken.IsCancellationRequested) return;

            callback?.Invoke(Mathf.Max(remaining, 0f), false);

            await UniTask.Delay(
                millisecondsDelay: 1000,
                ignoreTimeScale: false,
                delayTiming: PlayerLoopTiming.Update,
                cancellationToken: cancelToken,
                cancelImmediately: true);

            remaining -= 1f;
        }

        callback?.Invoke(0f, true);
        await UniTask.Yield(PlayerLoopTiming.Update);
    }
}
