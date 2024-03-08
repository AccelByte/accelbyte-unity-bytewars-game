// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System.IO;
using System.Linq;
using UnityEngine;
using System.Runtime.CompilerServices;

public static class BytewarsLogger
{
    public static void Log(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        string time = GetTime();
#if UNITY_SERVER
        Debug.Log($"[{time}] [{GetLast(sourceFilePath)}] [{memberName}] [Log] [{sourceLineNumber}] - {message}");
#else
        Debug.Log($"[{time}] [{Path.GetFileName(sourceFilePath)}] [{memberName}] [Log] [{sourceLineNumber}] - {message}");
#endif

    }
    public static void LogWarning(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        string time = GetTime();
#if UNITY_SERVER
        Debug.LogWarning($"[{time}] [{GetLast(sourceFilePath)}] [{memberName}] [Log] [{sourceLineNumber}] - {message}");
#else
        Debug.LogWarning($"[{time}] [{Path.GetFileName(sourceFilePath)}] [{memberName}] [Warning] [{sourceLineNumber}] - {message}");
#endif
    }

    private static string GetLast(string path)
    {
        var filename = path.Contains('\\') ? path.Split('\\').Last() : path.Split('/').Last();
        return filename;
    }

    private static string GetTime()
    {
        return System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}