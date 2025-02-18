// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.IO;
using System.Runtime.CompilerServices;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Unity.Netcode;
using UnityEngine;

public static class MatchSessionHelper
{
    private static ApiClient apiClient;
    private static User user;
    
    private static void Init()
    {
        if (user == null)
        {
            apiClient = AccelByteSDK.GetClientRegistry().GetApi();
            user = apiClient.GetApi<User, UserApi>();
        }
    }

    public static void RefreshLobbyMenu()
    {
        MenuCanvas currentMenu = MenuManager.Instance.GetCurrentMenu();
        if (currentMenu is MatchLobbyMenu matchLobbyMenu)
        {
            matchLobbyMenu.Refresh();
        }
    }

    #region GetCurrentUserPublicData

    public static void GetCurrentUserPublicData(string receivedUserId)
    {
        Init();
        GameData.CachedPlayerState.playerId = receivedUserId;
        user.GetUserByUserId(receivedUserId, OnGetUserPublicDataFinished);
    }

    private static void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"error OnGetUserPublicDataFinished:{result.Error.Message}");
        }
        else
        {
            PublicUserData publicUserData = result.Value;
            GameData.CachedPlayerState.playerId = publicUserData.userId;
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = publicUserData.displayName;
        }
    }

    #endregion GetCurrentUserPublicData

    #region LogResult

    public static void LogResult<T>(Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!result.IsError)
        {
            Debug.Log($"[{Path.GetFileName(sourceFilePath)}] [{memberName}] [Log] [{sourceLineNumber}] - {result.Value.ToJsonString()}");
        }
        else
        {
            Debug.LogWarning($"[{Path.GetFileName(sourceFilePath)}] [{memberName}] [Warning] [{sourceLineNumber}] - {result.Error.Message}");
        }
    }

    #endregion
}
