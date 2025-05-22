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
    public TokenData UserData;
    public static event Action<UserProfile> OnUserProfileReceived = delegate { };
    public UserProfile UserProfile { get; private set; }
    
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();

    // AGS Game SDK references
    private ApiClient apiClient;
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

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

        // Casting doesn't work properly, manually transfer the data instead.
        LoginWithDeviceIdV4OptionalParameters deviceIdOptionParameters = new LoginWithDeviceIdV4OptionalParameters
        {
            OnQueueUpdatedEvent = OptionalParameters.OnQueueUpdatedEvent,
            OnCancelledEvent = OptionalParameters.OnCancelledEvent,
            CancellationToken = OptionalParameters.CancellationToken
        };
        
        user.LoginWithDeviceIdV4(
            deviceIdOptionParameters,
            result => OnLoginCompleted(result, resultCallback));
    }

    public bool GetActiveUser()
    {
        return user?.Session?.IsValid() ?? false;
    }
    
    public void LoginWithUsername(string username, string password, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        BytewarsLogger.Log($"Trying login with email and password");
        
        // Casting doesn't work properly, manually transfer the data instead.
        LoginWithEmailV4OptionalParameters emailOptionParameters = new LoginWithEmailV4OptionalParameters
        {
            OnQueueUpdatedEvent = OptionalParameters.OnQueueUpdatedEvent,
            OnCancelledEvent = OptionalParameters.OnCancelledEvent,
            CancellationToken = OptionalParameters.CancellationToken
        };
        
        GameData.CachedPlayerState.PlatformId = "Accelbyte";
        user.LoginWithEmailV4(
            username, 
            password,
            emailOptionParameters,
            result => OnLoginCompleted(result, resultCallback));
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

            GameData.CachedPlayerState.PlayerId = result.Value.user_id;
            UserData = result.Value;

            GetUserProfile();
            CheckLobbyConnection();
        }
        else
        {
            BytewarsLogger.Log($"The user failed to log in with Device ID. Error Message: {result.Error.error}");
            GameData.CachedPlayerState.PlatformId = string.Empty;
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
        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { receivedUserId }, OnGetUserPublicDataFinished);
    }

    private void OnGetUserPublicDataFinished(Result<AccountUserPlatformInfosResponse> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Successfully Retrieved Public Data");
            AccountUserPlatformData publicUserData = result.Value.Data[0];
            string truncatedUserId = publicUserData.UserId[..5];
            GameData.CachedPlayerState.AvatarUrl = publicUserData.AvatarUrl;
            GameData.CachedPlayerState.PlayerName = string.IsNullOrEmpty(publicUserData.DisplayName) ?
                $"Player-{truncatedUserId}" : publicUserData.DisplayName;
            GameData.CachedPlayerState.PlatformId = string.IsNullOrEmpty(GameData.CachedPlayerState.PlatformId) ? UserData.platform_id : GameData.CachedPlayerState.PlatformId;
        }
        else
        {
            BytewarsLogger.Log($"Error:{result.Error.Message}");
        }
    }

    #endregion
}
