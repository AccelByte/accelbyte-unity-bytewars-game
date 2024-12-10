// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class UserDataHelper
{
    private static ApiClient apiClient;
    private static User user;
    private const string ClassName = "[UserDataHelper]";

    public static void OnLoginSuccess(TokenData tokenData)
    {
        Init();
        GameData.CachedPlayerState.playerId = tokenData.user_id;
        user.GetUserByUserId(tokenData.user_id, OnGetUserPublicDataFinished);
    }

    private static void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            Debug.Log($"{ClassName} error OnGetUserPublicDataFinished:{result.Error.Message}");
        }
        else
        {
            var publicUserData = result.Value;
            GameData.CachedPlayerState.playerId = publicUserData.userId;
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = publicUserData.displayName;
        }
    }

    private static void Init()
    {
        if (user != null)
        {
            return;
        }
        apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        user = apiClient.GetApi<User, UserApi>();
    }
}
