// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SinglePlatformAuthWrapper : MonoBehaviour
{
    private const string ClassName = "SinglePlatformAuthWrapper";
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;
    private LoginHandler loginHandler = null;
    private SteamHelper steamHelper;
    private const PlatformType PlatformType = AccelByte.Models.PlatformType.Steam;
    private ResultCallback<TokenData, OAuthError> platformLoginCallback;

    private TokenData tokenData;
    public UserProfile UserProfile { get; private set; }

    public static event Action<UserProfile> OnUserProfileReceived = delegate { };
    
    private void Start()
    {
        ApiClient apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        user = apiClient.GetUser();
        userProfiles = apiClient.GetUserProfiles();
        lobby = apiClient.GetLobby();

        steamHelper = new SteamHelper();

        SetLoginWithSteamButtonClickCallback();
    }

    private void OnApplicationQuit()
    {
        if (lobby.IsConnected)
        {
            lobby.Disconnect();
        }
    }

    private void SetLoginWithSteamButtonClickCallback()
    {
        if (loginHandler == null)
        {
            if (MenuManager.Instance != null)
            {
                var menuCanvas = MenuManager.Instance.GetMenu(AssetEnum.LoginMenuCanvas);
                if (menuCanvas != null && menuCanvas is LoginHandler loginHandlerC)
                {
                    loginHandler = loginHandlerC;
                    var loginWithSteamButton = loginHandler.GetLoginButton(LoginType.Steam);
                    bool isSingleAuthModuleActive =
                        TutorialModuleManager.Instance.IsModuleActive(TutorialType.SinglePlatformAuth);
                    bool isLoginWithSteam = isSingleAuthModuleActive && SteamManager.Initialized;
                    if (isLoginWithSteam)
                    {
                        if (GConfig.GetSteamAutoLogin())
                        {
                            OnLoginWithSteamButtonClicked();
                        }
                        else
                        {
                            loginWithSteamButton.onClick.AddListener(OnLoginWithSteamButtonClicked);
                            loginWithSteamButton.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    private void OnLoginWithSteamButtonClicked()
    {
        if (loginHandler == null) return;
        loginHandler.OnRetryLoginClicked = OnLoginWithSteamButtonClicked;
        loginHandler.SetView(LoginHandler.LoginView.LoginLoading);
        //get steam token to be used as platform token later
        steamHelper.GetAuthSessionTicket(OnGetAuthSessionTicketFinished);
    }

    private void GetUserPublicData(string receivedUserId)
    {
        GameData.CachedPlayerState.playerId = receivedUserId;
        user.GetUserByUserId(receivedUserId, OnGetUserPublicDataFinished);
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
    
    private void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            Debug.Log($"[{ClassName}] error OnGetUserPublicDataFinished:{result.Error.Message}");
            loginHandler.OnRetryLoginClicked = () => GetUserPublicData(tokenData.user_id);
            loginHandler.OnLoginCompleted(CreateLoginErrorResult(result.Error.Code, result.Error.Message));
        }
        else
        {
            var publicUserData = result.Value;
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = publicUserData.displayName;
            loginHandler.OnLoginCompleted(Result<TokenData,OAuthError>.CreateOk(tokenData));
        }
    }

    private void OnGetAuthSessionTicketFinished(string steamAuthSessionTicket)
    {
        if (loginHandler == null) return;
        if (String.IsNullOrEmpty(steamAuthSessionTicket))
        {
            loginHandler.OnLoginCompleted(
                CreateLoginErrorResult(ErrorCode.CachedTokenNotFound, 
                    "Failed to get steam token"));
        }
        else
        {
            //login with platform token
            user.LoginWithOtherPlatform(PlatformType, 
                steamAuthSessionTicket, OnLoginWithOtherPlatformCompleted);
        }
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

    private void OnLoginWithOtherPlatformCompleted(Result<TokenData, OAuthError> result)
    {
        if (result.IsError)
        {
            loginHandler.OnLoginCompleted(result);
        }
        else
        {
            tokenData = result.Value;
            GetUserPublicData(tokenData.user_id);

            GetUserProfile();
            CheckLobbyConnection();
        }
    }

    private void CheckLobbyConnection()
    {
        if (!lobby.IsConnected)
        {
            lobby.Connected += OnLobbyConnected;
            lobby.Disconnected += OnLobbyDisconnected;
            lobby.Disconnecting += OnLobbyDisconnecting;
            lobby.Connect();
        }
    }

    private void OnLobbyConnected()
    {
        BytewarsLogger.Log($"Lobby service connected: {lobby.IsConnected}");
    }

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
        }
    }

    private void OnLobbyDisconnecting(Result<DisconnectNotif> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.Log($"Error disconnecting lobby: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Lobby disconnected: {result.Value}");
    }

    private Result<TokenData, OAuthError> CreateLoginErrorResult(ErrorCode errorCode, string errorDescription)
    {
        return Result<TokenData, OAuthError>.CreateError(new OAuthError()
        {
            error = errorCode.ToString(),
            error_description = errorDescription
        });
    }
}
