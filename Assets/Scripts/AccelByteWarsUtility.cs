// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Globalization;
using UnityEngine;
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
}
