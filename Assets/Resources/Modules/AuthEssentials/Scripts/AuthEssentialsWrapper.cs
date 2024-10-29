// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper : MonoBehaviour
{

    public static event Action<UserProfile> OnUserProfileReceived = delegate { };
    public TokenData UserData;
    public UserProfile UserProfile { get; private set; }

    // AGS Game SDK references
    private ApiClient apiClient;
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;
    
    // required variables to login with other platform outside AccelByte
    private PlatformType platformType;
    private string platformToken;

    private void Awake()
    {
        apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        user = apiClient.GetUser();
        userProfiles = apiClient.GetUserProfiles();
        lobby = apiClient.GetLobby();
    }

    void OnApplicationQuit()
    {
        lobby.Disconnect();
    }

    void OnDestroy()
    {
        lobby.Disconnecting -= OnLobbyDisconnecting;
    }

    #region AB Service Functions

    public void LoginWithDeviceId(ResultCallback<TokenData, OAuthError> resultCallback)
    {
        BytewarsLogger.Log($"Trying to login with Device ID");
        user.LoginWithDeviceId(result => OnLoginCompleted(result, resultCallback));
    }

    public void LoginWithUsername(string username, string password, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        user.LoginWithUsernameV3(username, password, result => OnLoginCompleted(result, resultCallback), false);
    }

    public void CheckLobbyConnection()
    {
        if (!lobby.IsConnected)
        {
            lobby.Connected += OnLobbyConnected;
            lobby.Disconnected += OnLobbyDisconnected;
            lobby.Disconnecting += OnLobbyDisconnecting;
            lobby.Connect();
        }
    }

    public void GetUserByUserId(string userId, ResultCallback<PublicUserData> resultCallback)
    {
        user.GetUserByUserId(userId, result => OnGetUserByUserId(result, resultCallback));
    }

    public void Logout(Action action)
    {
        user.Logout(result => OnLogoutComplete(result, action));
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

    /// <summary>
    /// Get user info of some users in bulk
    /// </summary>
    /// <param name="userIds">an array of user id from the desired users</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void BulkGetUserInfo(string[] userIds, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        user.BulkGetUserInfo(userIds, result => OnBulkGetUserInfoCompleted(result, resultCallback));
    }

    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        user.GetUserAvatar(userId, result => OnGetUserAvatarCompleted(result, resultCallback));
    }

    private void GetUserProfile()
    {
        userProfiles.GetUserProfile(result => OnGetUserProfileCompleted(result));
    }

    private void CreateUserProfile()
    {
        string twoLetterLanguageCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        string timeZoneId = TutorialModuleUtil.GetLocalTimeOffsetFromUTC();

        CreateUserProfileRequest createUserProfileRequest = new()
        {
            language = twoLetterLanguageCode,
            timeZone = timeZoneId,
        };

        userProfiles.CreateUserProfile(createUserProfileRequest, result => OnCreateUserProfileCompleted(result));
    }

    #endregion
    
    #region Lobby Callback
    /// <summary>
    /// A callback function to handle websocket connection closed events from the lobby service
    /// </summary>
    /// <param name="code"></param>
    private void OnLobbyDisconnected(WsCloseCode code)
    {
        BytewarsLogger.Log($"Lobby service disconnected with code: {code}");

        HashSet<WsCloseCode> loginDisconnectCodes = new()
        {
            WsCloseCode.Normal,
            WsCloseCode.DisconnectDueToMultipleSessions,
            WsCloseCode.DisconnectDueToIAMLoggedOut
        };

        if (loginDisconnectCodes.Contains(code))
        {
            lobby.Connected -= OnLobbyConnected;
            lobby.Disconnected -= OnLobbyDisconnected;

            AuthEssentialsHelper.OnUserLogout?.Invoke();
        }
    }

    private void OnLobbyDisconnecting(Result<DisconnectNotif> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"{result.Value.message}");
        } 
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
    }

    private void OnLobbyConnected()
    {
        BytewarsLogger.Log($"Lobby service connected: {lobby.IsConnected}");
    }

    #endregion

    #region Callback Functions

    /// <summary>
    /// Default Callback for LoginWithOtherPlatform() function
    /// </summary>
    /// <param name="result">result of the LoginWithOtherPlatform() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnLoginCompleted(Result<TokenData, OAuthError> result, ResultCallback<TokenData, OAuthError> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"The user successfully logged in with Device ID: {result.Value.platform_user_id}");

            GameData.CachedPlayerState.playerId = result.Value.user_id;
            UserData = result.Value;

            GetUserProfile();
            CheckLobbyConnection();
        }
        else
        {
            BytewarsLogger.Log($"The user failed to log in with Device ID. Error Message: {result.Error.error}");
        }

        customCallback?.Invoke(result);
    }

    private void OnGetUserByUserId(Result<PublicUserData> result, ResultCallback<PublicUserData> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully get the user public data!");
        }
        else
        {
            BytewarsLogger.Log($"Unable to get the user public data. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    /// <summary>
    /// Default Callback for BulkGetUserInfo() function
    /// </summary>
    /// <param name="result">result of the BulkGetUserInfo() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnBulkGetUserInfoCompleted(Result<ListBulkUserInfoResponse> result, ResultCallback<ListBulkUserInfoResponse> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully bulk get the user info!");
        }
        else
        {
            BytewarsLogger.Log($"Unable to bulk get the user info. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnGetUserAvatarCompleted(Result<Texture2D> result, ResultCallback<Texture2D> customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully retrieve the user avatar! ");
        }
        else
        {
            BytewarsLogger.LogWarning($"Unable to retrieve the user avatar. Message: {result.Error}");
        }

        customCallback?.Invoke(result);
    }

    private void OnGetUserProfileCompleted(Result<UserProfile> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully retrieved self user profile!");

            UserProfile = result.Value;
            GetUserPublicData(UserProfile.userId);
            OnUserProfileReceived?.Invoke(UserProfile);
        }
        else
        {
            BytewarsLogger.Log($"Unable to retrieve self user profile. Message: {result.Error.Message}");

            if (result.Error.Code is ErrorCode.UserProfileNotFoundException)
            {
                CreateUserProfile();
            }
        }
    }

    private void OnCreateUserProfileCompleted(Result<UserProfile> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully created self user profile!");

            UserProfile = result.Value;
            OnUserProfileReceived?.Invoke(UserProfile);
        }
        else
        {
            BytewarsLogger.Log($"Unable to create self user profile. Message: {result.Error.Message}");
        }
    }

    private void GetUserPublicData(string receivedUserId)
    {
        user.GetUserByUserId(receivedUserId, OnGetUserPublicDataFinished);
    }

    private void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully Retrieved Public Data");
            PublicUserData publicUserData = result.Value;
            string truncatedUserId = publicUserData.userId[..5];
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = string.IsNullOrEmpty(publicUserData.displayName) ?
                $"Player-{truncatedUserId}" : publicUserData.displayName;
        }
        else
        {
            BytewarsLogger.Log($"Error:{result.Error.Message}");
        }
    }

    #endregion
}
