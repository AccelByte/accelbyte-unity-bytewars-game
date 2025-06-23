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

public class SinglePlatformAuthWrapper_Starter : MonoBehaviour
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

        // TODO: Add the tutorial module code here.
    }

    void OnDestroy()
    {
        // TODO: Add the tutorial module code here.
    }

    private async void AssignSinglePlatformAuthButtonCallback()
    {
        while (!(MenuManager.Instance?.IsInitiated ?? false) ||
            MenuManager.Instance?.GetCurrentMenu()?.GetAssetEnum() != AssetEnum.LoginMenu_Starter)
        {
            await UniTask.Yield();
        }

        LoginMenu_Starter loginMenu = MenuManager.Instance.GetCurrentMenu() as LoginMenu_Starter;
        Button loginButton = loginMenu?.GetLoginButton(AuthEssentialsModels.LoginType.SinglePlatformAuth);
        TMP_Text loginButtonText = loginButton?.GetComponentInChildren<TMP_Text>();
        if (!loginMenu || !loginButton || !loginButtonText)
        {
            return;
        }

        // Here, it set Steam as the default single platform auth. You can add more third-party integration if needed.
        PlatformType targetPlatformType = PlatformType.Steam;
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

    // TODO: Declare your get Steam auth ticket function here.

    private void OnLoginWithSteamButtonClicked()
    {
        // TODO: Add the tutorial module code here.
    }

    #endregion
#endif

    // TODO: Declare the tutorial module functions here.
}
