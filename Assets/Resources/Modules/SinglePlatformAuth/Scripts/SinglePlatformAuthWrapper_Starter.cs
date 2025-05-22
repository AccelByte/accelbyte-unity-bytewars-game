// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SinglePlatformAuthWrapper_Starter : MonoBehaviour
{
    public static event Action<UserProfile> OnUserProfileReceived = delegate { };
    
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();
    
    private const string ClassName = "SinglePlatformAuthWrapper_Starter";
    private User user;
    private LoginHandler loginHandler = null;
    private SteamHelper steamHelper;
    private LoginPlatformType platformType = new LoginPlatformType(AccelByte.Models.PlatformType.Steam);
    private ResultCallback<TokenData, OAuthError> platformLoginCallback;
    private TokenData tokenData;

    private void Start()
    {
        BytewarsLogger.Log($"[{ClassName}] is started");
        steamHelper = new SteamHelper();
        var apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        user = apiClient.GetApi<User, UserApi>();
        SetLoginWithSteamButtonClickCallback();
    }
    private void OnLoginWithSteamButtonClicked()
    {
        // TODO: login to steam platform and pass the steam ticket over to AGS login
    }

    private void SetLoginWithSteamButtonClickCallback()
    {
        // TODO: this function will get login with steam button reference and assign onClick callback to it
    }
}
