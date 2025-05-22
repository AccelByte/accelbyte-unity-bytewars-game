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
        GameData.CachedPlayerState.PlayerId = receivedUserId;
        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { receivedUserId }, OnGetUserPublicDataFinished);
    }

    private static void OnGetUserPublicDataFinished(Result<AccountUserPlatformInfosResponse> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to get user public data info. Error: {result.Error.Message}");
        }
        else
        {
            AccountUserPlatformData publicUserData = result.Value.Data[0];
            GameData.CachedPlayerState.PlayerId = publicUserData.UserId;
            GameData.CachedPlayerState.AvatarUrl = publicUserData.AvatarUrl;
            GameData.CachedPlayerState.PlayerName = publicUserData.DisplayName;
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
