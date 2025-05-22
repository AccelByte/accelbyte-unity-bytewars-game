// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper_Starter : MonoBehaviour
{
    //TODO: Copy UserData here
    public UserProfile UserProfile { get; private set; }
    public static event Action<UserProfile> OnUserProfileReceived = delegate { };
    
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();

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
        }
        else
        {
            BytewarsLogger.Log($"Error:{result.Error.Message}");
        }
    }

    #endregion
}
