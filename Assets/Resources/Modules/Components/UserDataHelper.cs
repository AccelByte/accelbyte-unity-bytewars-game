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
        GameData.CachedPlayerState.PlayerId = tokenData.user_id;
        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { tokenData.user_id }, OnGetUserPublicDataFinished);
    }

    private static void OnGetUserPublicDataFinished(Result<AccountUserPlatformInfosResponse> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to get user public data info. Error:{result.Error.Message}");
        }
        else
        {
            AccountUserPlatformData publicUserData = result.Value.Data[0];
            GameData.CachedPlayerState.PlayerId = publicUserData.UserId;
            GameData.CachedPlayerState.AvatarUrl = publicUserData.AvatarUrl;
            GameData.CachedPlayerState.PlayerName = publicUserData.DisplayName;
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
