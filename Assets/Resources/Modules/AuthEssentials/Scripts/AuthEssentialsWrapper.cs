// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private ApiClient apiClient;
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;
    
    // required variables to login with other platform outside AccelByte
    private PlatformType platformType;
    private string platformToken;

    public TokenData userData;
    public UserProfile UserProfile { get; private set; }

    public static event Action<UserProfile> OnUserProfileReceived = delegate { };

    private void Awake()
    {
        apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        user = apiClient.GetApi<User, UserApi>();
        userProfiles = apiClient.GetApi<UserProfiles, UserProfilesApi>();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        lobby.Disconnected += OnLobbyDisconnected;
        lobby.Disconnecting += OnLobbyDisconnecting;

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

    public void Login(LoginType loginMethod, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        switch (loginMethod)
        {
            case LoginType.DeviceId:
                platformType = PlatformType.Device;
                platformToken = SystemInfo.deviceUniqueIdentifier;
                break;
            case LoginType.Steam:
                break;
            default:
                break;
        }

        BytewarsLogger.Log($"[AuthEssentials] Trying to login with device id: {SystemInfo.deviceUniqueIdentifier}");

        user.LoginWithOtherPlatform(platformType, platformToken, result => OnLoginCompleted(result, resultCallback));
    }

    public void LoginWithUsername(string username, string password, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        user.LoginWithUsernameV3(username, password, result => OnLoginCompleted(result, resultCallback), false);
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
        apiClient.GetApi<UserProfiles, UserProfilesApi>().GetUserProfile(result => OnGetUserProfileCompleted(result));
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

        switch (code)
        {
            case WsCloseCode.Normal:
                BytewarsLogger.Log($"{code}");
                AuthEssentialsHelper.OnUserLogout?.Invoke();
                lobby.Connected -= OnConnected;
                lobby.Disconnected -= OnLobbyDisconnected;
                break;
            case WsCloseCode.DisconnectDueToMultipleSessions:
                BytewarsLogger.Log($"{code}");
                AuthEssentialsHelper.OnUserLogout?.Invoke();
                lobby.Connected -= OnConnected;
                lobby.Disconnected -= OnLobbyDisconnected;
                break;
            case WsCloseCode.DisconnectDueToIAMLoggedOut:
                BytewarsLogger.Log($"{code}");
                AuthEssentialsHelper.OnUserLogout?.Invoke();
                lobby.Connected -= OnConnected;
                lobby.Disconnected -= OnLobbyDisconnected;
                break;
            case WsCloseCode.Undefined:
                BytewarsLogger.Log($"{code}");
                break;
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

    private void OnConnected()
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
            BytewarsLogger.Log("Login user successful.");

            GameData.CachedPlayerState.playerId = result.Value.user_id;
            userData = result.Value;

            GetUserProfile();
            if (!lobby.IsConnected)
            {
                lobby.Connect();
                lobby.Connected += OnConnected;
            }
        }
        else
        {
            BytewarsLogger.Log($"Login user failed. Message: {result.Error.error}");
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
            OnUserProfileReceived?.Invoke(UserProfile);
        }
        else
        {
            BytewarsLogger.Log($"Unable to retrieve self user profile. Message: {result.Error.Message}");

            //TODO: Nicky (5/15/2024) Change the condition to check for
            //      the correct error code when implemented in the SDK.
            const string userProfileNotFoundCode = "11440";
            if (result.Error.Code.ToString() is userProfileNotFoundCode)
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

    #endregion
}
