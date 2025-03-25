// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper_Starter : MonoBehaviour
{
    //TODO: Copy UserData here
    public UserProfile UserProfile { get; private set; }
    public static event Action<UserProfile> OnUserProfileReceived = delegate { };

    // AGS Game SDK references
    private ApiClient apiClient;
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;
 
    //TODO: Copy platformType and platformToken here

    //TODO: Copy OnloginCompleted() here from "Use the AccelByte SDK to Login" unit here

    //TODO: Copy Login() here from "Use the AccelByte SDK to Login" unit here

    
    #region AB Service Functions

    void OnApplicationQuit()
    {
        lobby.Disconnect();
    }

    void OnDestroy()
    {
        lobby.Disconnecting -= OnLobbyDisconnecting;
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
            GameData.CachedPlayerState.AvatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.PlayerName = string.IsNullOrEmpty(publicUserData.displayName) ?
                $"Player-{truncatedUserId}" : publicUserData.displayName;
        }
        else
        {
            BytewarsLogger.Log($"Error:{result.Error.Message}");
        }
    }

    #endregion
}
