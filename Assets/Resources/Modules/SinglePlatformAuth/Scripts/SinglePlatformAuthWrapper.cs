// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
#if !UNITY_WEBGL
using Steamworks;
#endif

public class SinglePlatformAuthWrapper : MonoBehaviour
{
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();

    // AGS Game SDK references
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

    private void Awake()
    {
        AssignSinglePlatformAuthButtonCallback();

        user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        userProfiles = AccelByteSDK.GetClientRegistry().GetApi().GetUserProfiles();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
    }

    private async void AssignSinglePlatformAuthButtonCallback()
    {
        while (!(MenuManager.Instance?.IsInitiated ?? false) ||
            MenuManager.Instance?.GetCurrentMenu()?.GetAssetEnum() != AssetEnum.LoginMenu)
        {
            await UniTask.Yield();
        }

        LoginMenu loginMenu = MenuManager.Instance.GetCurrentMenu() as LoginMenu;
        Button loginButton = loginMenu?.GetLoginButton(AuthEssentialsModels.LoginType.SinglePlatformAuth);
        TMP_Text loginButtonText = loginButton?.GetComponentInChildren<TMP_Text>();
        if (!loginMenu || !loginButton || !loginButtonText)
        {
            return;
        }

        // Get the default single platform auth from config file.
        if (!Enum.TryParse(ConfigurationReader.Config?.singlePlatformAuth ?? string.Empty, out PlatformType targetPlatformType))
        {
            targetPlatformType = PlatformType.None;
        }

        // Set callback function based on the target single platform auth. You can add more third-party integration if needed.
        loginButtonText.text = $"Login with {targetPlatformType.ToString()}";
        loginButton.gameObject.SetActive(true);
        switch (targetPlatformType)
        {
#if !UNITY_WEBGL
            case PlatformType.Steam:
                loginButton.onClick.AddListener(OnLoginWithSteamButtonClicked);
                loginButton.gameObject.SetActive(SteamManager.Initialized);
                if (GConfig.GetSteamAutoLogin())
                {
                    loginButton.onClick.Invoke();
                }
                break;
#endif
            default:
                loginButton.gameObject.SetActive(false);
                break;
        }
    }

#if !UNITY_WEBGL
    #region Steam Functions

    private void OnLoginWithSteamButtonClicked()
    {
        LoginMenu loginMenu = MenuManager.Instance.GetCurrentMenu() as LoginMenu;
        if (!loginMenu)
        {
            return;
        }

        loginMenu.OnRetryLoginClicked = OnLoginWithSteamButtonClicked;
        loginMenu.WidgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        // Get steam token to be used as platform token later
        GetSteamAuthTicket((Result<string> result) =>
        {
            if (result.IsError)
            {
                loginMenu.OnLoginCompleted(Result<TokenData, OAuthError>.CreateError(new OAuthError() { error = result.Error.Message }));
                return;
            }

            // Login to AccelByte with Steam auth ticket.
            LoginWithOtherPlatform(result.Value, PlatformType.Steam, loginMenu.OnLoginCompleted);
        });
    }

    private async void GetSteamAuthTicket(ResultCallback<string> resultCallback)
    {
        if (!SteamManager.Initialized)
        {
            BytewarsLogger.LogWarning(
                "Failed to get Steam auth ticket. Steam API is not initialized. " +
                "Try to open the Steam Client first before launching the game.");
            resultCallback?.Invoke(Result<string>.CreateError(ErrorCode.NotImplemented, "Steam API is not initialized"));
            return;
        }

        byte[] buffer = new byte[1024];
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetGenericString(string.Empty);
        
        // Request to get Steam auth ticket.
        HAuthTicket request = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out uint ticketSize, ref identity);
        Array.Resize(ref buffer, (int)ticketSize);

        // Set request callback.
        TaskCompletionSource<string> getAuthTicketTask = new TaskCompletionSource<string>();
        Callback<GetAuthSessionTicketResponse_t> callback = Callback<GetAuthSessionTicketResponse_t>.Create((GetAuthSessionTicketResponse_t response) =>
        {
            if (response.m_hAuthTicket == request && response.m_eResult == EResult.k_EResultOK)
            {
                string sessionTicket = BitConverter.ToString(buffer).Replace("-", string.Empty);
                getAuthTicketTask.TrySetResult(sessionTicket);
            }
            else
            {
                getAuthTicketTask.TrySetResult(null);
            }
        });

        // Return Steam auth ticket.
        string authTicket = await getAuthTicketTask.Task;
        if (string.IsNullOrEmpty(authTicket))
        {
            resultCallback?.Invoke(Result<string>.CreateError(ErrorCode.UnknownError, "Failed to get Steam Auth Ticket"));
        }
        else
        {
            resultCallback?.Invoke(Result<string>.CreateOk(authTicket));
        }
    }

    #endregion
#endif

    #region AB Service Functions

    public void LoginWithOtherPlatform(
        string platformToken, 
        PlatformType platformType, 
        ResultCallback<TokenData, OAuthError> resultCallback)
    {
        LoginWithOtherPlatformV4OptionalParameters optionalParams = new LoginWithOtherPlatformV4OptionalParameters
        {
            OnQueueUpdatedEvent = OptionalParameters.OnQueueUpdatedEvent,
            OnCancelledEvent = OptionalParameters.OnCancelledEvent,
            CancellationToken = OptionalParameters.CancellationToken
        };

        user.LoginWithOtherPlatformV4(
            new LoginPlatformType(platformType),
            platformToken,
            optionalParams,
            result => OnLoginWithOtherPlatformCompleted(result, resultCallback));
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

    private void OnLoginWithOtherPlatformCompleted(
        Result<TokenData, OAuthError> loginResult, 
        ResultCallback<TokenData, OAuthError> resultCallback = null)
    {
        // Abort if failed to login.
        if (loginResult.IsError)
        {
            BytewarsLogger.Log($"Failed to login with other platform. Error: {loginResult.Error.error}");
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
        BytewarsLogger.Log("Success to login with other platform. Querying the user profile and user info.");
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

    #endregion
}
