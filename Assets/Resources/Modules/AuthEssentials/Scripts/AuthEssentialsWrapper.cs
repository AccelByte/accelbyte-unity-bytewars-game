// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper : MonoBehaviour
{
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();

    // AGS Game SDK references
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

    private void Awake()
    {
        user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        userProfiles = AccelByteSDK.GetClientRegistry().GetApi().GetUserProfiles();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        lobby.Disconnected += OnLobbyDisconnected;
        MainMenu.OnQuitPressed += Logout;
    }

    #region AB Service Functions

    public void LoginWithDeviceId(ResultCallback<TokenData, OAuthError> resultCallback)
    {
        LoginWithDeviceIdV4OptionalParameters optionalParams = new LoginWithDeviceIdV4OptionalParameters
        {
            OnQueueUpdatedEvent = OptionalParameters.OnQueueUpdatedEvent,
            OnCancelledEvent = OptionalParameters.OnCancelledEvent,
            CancellationToken = OptionalParameters.CancellationToken
        };
        
        user.LoginWithDeviceIdV4(
            optionalParams, 
            result => OnLoginCompleted(result, resultCallback));
    }
    
    public void LoginWithUsername(
        string username, 
        string password, 
        ResultCallback<TokenData, OAuthError> resultCallback)
    {
        LoginWithEmailV4OptionalParameters optionalParams = new LoginWithEmailV4OptionalParameters
        {
            OnQueueUpdatedEvent = OptionalParameters.OnQueueUpdatedEvent,
            OnCancelledEvent = OptionalParameters.OnCancelledEvent,
            CancellationToken = OptionalParameters.CancellationToken
        };
        
        GameData.CachedPlayerState.PlatformId = "ACCELBYTE";
        user.LoginWithEmailV4(
            username, 
            password,
            optionalParams,
            result => OnLoginCompleted(result, resultCallback));
    }

    public void Logout(Action action)
    {
        user.Logout(result => OnLogoutComplete(result, action));
    }

    private void CreateOrGetUserProfile(ResultCallback<UserProfile> resultCallback)
    {
        userProfiles.GetUserProfile((Result<UserProfile> getUserProfileResult) =>
        {
            // If not found because it is not yet created, then try to create one.
            if (getUserProfileResult.IsError &&
                getUserProfileResult.Error.Code == ErrorCode.UserProfileNotFoundException)
            {
                CreateUserProfileRequest request = new CreateUserProfileRequest()
                {
                    language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    timeZone = TutorialModuleUtil.GetLocalTimeOffsetFromUTC(),
                };
                userProfiles.CreateUserProfile(request, resultCallback);
                return;
            }

            resultCallback.Invoke(getUserProfileResult);
        });
    }

    private void GetUserPublicData(string userId, ResultCallback<AccountUserPlatformInfosResponse> resultCallback)
    {
        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { userId }, resultCallback);
    }

    #endregion

    #region Callback Functions

    private void OnLoginCompleted(
        Result<TokenData, OAuthError> loginResult, 
        ResultCallback<TokenData, OAuthError> resultCallback = null)
    {
        // Abort if failed to login.
        if (loginResult.IsError) 
        {
            BytewarsLogger.Log($"Failed to login. Error: {loginResult.Error.error}");
            resultCallback?.Invoke(loginResult);
            return;
        }

        // Connect to lobby if haven't.
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }

        // Get user profile.
        TokenData tokenData = loginResult.Value;
        BytewarsLogger.Log("Success to login. Querying the user profile and user info.");
        CreateOrGetUserProfile((Result<UserProfile> userProfileResult) =>
        {
            // Abort if failed to create or get user profile.
            if (userProfileResult.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to create or get user profile. Error: {userProfileResult.Error.Message}");
                resultCallback?.Invoke(Result<TokenData, OAuthError>.CreateError(new OAuthError() { error = userProfileResult.Error.Message }));
                return;
            }

            // Get user public info.
            GetUserPublicData(tokenData.user_id, (Result<AccountUserPlatformInfosResponse> userInfoResult) =>
            {
                // Abort if failed to get user info.
                if (userInfoResult.IsError || userInfoResult.Value.Data.Length <= 0)
                {
                    BytewarsLogger.LogWarning($"Failed to get user info. Error: {userInfoResult.Error.Message}");
                    resultCallback?.Invoke(Result<TokenData, OAuthError>.CreateError(new OAuthError() { error = userInfoResult.Error.Message }));
                    return;
                }

                // Save the public user info in the game cache.
                AccountUserPlatformData publicUserData = userInfoResult.Value.Data[0];
                GameData.CachedPlayerState.PlayerId = publicUserData.UserId;
                GameData.CachedPlayerState.AvatarUrl = publicUserData.AvatarUrl;
                GameData.CachedPlayerState.PlayerName =
                    string.IsNullOrEmpty(publicUserData.DisplayName) ?
                    $"Player-{publicUserData.UserId[..5]}" :
                    publicUserData.DisplayName;
                GameData.CachedPlayerState.PlatformId =
                    string.IsNullOrEmpty(GameData.CachedPlayerState.PlatformId) ?
                    tokenData.platform_id : GameData.CachedPlayerState.PlatformId;

                resultCallback.Invoke(loginResult);
            });
        });
    }

    private void OnLogoutComplete(Result result, Action callback)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Logout successful");
            callback?.Invoke();
        }
        else
        {
            BytewarsLogger.Log($"Logout failed. Error message: {result.Error.Message}");
        }
    }

    private void OnLobbyDisconnected(WsCloseCode code)
    {
        BytewarsLogger.Log($"Lobby service disconnected with code: {code}");

        /* If disconnected from lobby intentionally or due to account issue.
         * Log out and back to the login menu.*/
        HashSet<WsCloseCode> disconnectCode = new HashSet<WsCloseCode>()
        {
            WsCloseCode.Normal,
            WsCloseCode.DisconnectDueToMultipleSessions,
            WsCloseCode.DisconnectDueToIAMLoggedOut
        };
        if (disconnectCode.Contains(code))
        {
            lobby.Disconnected -= OnLobbyDisconnected;
            Logout(() => { MenuManager.Instance.ChangeToMenu(AssetEnum.LoginMenu); });
        }
    }

    #endregion
}
