// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class LoginMenu : MenuCanvas
{
    public UnityAction OnRetryLoginClicked
    {
        set
        {
            retryLoginButton.onClick.RemoveAllListeners();
            retryLoginButton.onClick.AddListener(value);
        }
    }

    public AccelByteWarsWidgetSwitcher WidgetSwitcher { get { return widgetSwitcher; } }

    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Button loginWithDeviceIdButton;
    [SerializeField] private Button loginWithSinglePlatformAuthButton;
    [SerializeField] private Button retryLoginButton;
    [SerializeField] private Button quitGameButton;

    private AuthEssentialsWrapper authWrapper;
    private string username = string.Empty;
    private string password = string.Empty;

    private void Start()
    {
        // Get auth's subsystem
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        loginWithDeviceIdButton.onClick.AddListener(OnLoginWithDeviceIdButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);
    }

    private void OnEnable()
    {
        StartCoroutine(CheckWrapper(AutoLogin));
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
    }

    private void OnDisable()
    {
        StopCoroutine(CheckWrapper(AutoLogin));
    }

    private void OnLoginWithDeviceIdButtonClicked()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        OnRetryLoginClicked = OnLoginWithDeviceIdButtonClicked;
        authWrapper.LoginWithDeviceId(OnLoginCompleted);
    }

    public void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (!result.IsError)
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.MainMenuCanvas);
            BytewarsLogger.Log($"Login success: {result.Value.ToJsonString()}");
        }
        else
        {
            widgetSwitcher.ErrorMessage = $"Login failed: {result.Error.error}";
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            StartCoroutine(SetSelectedGameObject(retryLoginButton.gameObject));
        }
    }

    private void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    IEnumerator CheckWrapper(Action action)
    {
        while (authWrapper == null)
        {
            yield return null; // Wait for the next frame
        }

        action.Invoke();
    }

    IEnumerator SetSelectedGameObject(GameObject gameObjectToSelect)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(gameObjectToSelect);
    }

    public override GameObject GetFirstButton()
    {
        return loginWithDeviceIdButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LoginMenu;
    }
    
    public Button GetLoginButton(AuthEssentialsModels.LoginType loginType)
    {
        switch (loginType)
        {
            case AuthEssentialsModels.LoginType.DeviceId:
                return loginWithDeviceIdButton;
            case AuthEssentialsModels.LoginType.SinglePlatformAuth:
                return loginWithSinglePlatformAuthButton;
        }
        return null;
    }

    #region Login with username (for debugging purpose)
    private void AutoLogin()
    {
        if (MenuManager.Instance.GetCurrentMenu().GetType() == GetType())
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AutoLoginURL();
#else
            AutoLoginCmd();
#endif
        }
    }

    private void AutoLoginCmd()
    {
        bool argCheckingLoginWithUsername = Environment.GetCommandLineArgs().Contains("-AUTH_TYPE=ACCELBYTE");
        string[] cmdArgs = Environment.GetCommandLineArgs();

#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            argCheckingLoginWithUsername = ClonesManager.GetArgument().Contains("-AUTH_TYPE=ACCELBYTE");
            cmdArgs = ClonesManager.GetArgument().Split();
        }
#endif
        if (!argCheckingLoginWithUsername)
        {
            return;
        }

        // Retrieve username / email and password from launch param
        username = string.Empty;
        password = string.Empty;
        foreach (string cmdArg in cmdArgs)
        {
            if (cmdArg.Contains("-AUTH_LOGIN="))
            {
                username = cmdArg.Replace("-AUTH_LOGIN=", "");
            }

            if (cmdArg.Contains("-AUTH_PASSWORD="))
            {
                password = cmdArg.Replace("-AUTH_PASSWORD=", "");
            }
        }

        OnLoginWithUsername();
    }

    private void AutoLoginURL()
    {
        Dictionary<string, string> urlParams = ConnectionHandler.GetURLParameters();
        if (urlParams.TryGetValue("-AUTH_TYPE", out string authType) && authType == "ACCELBYTE")
        {
            // Retrieve username / email and password from URL
            urlParams.TryGetValue("-AUTH_LOGIN", out username);
            urlParams.TryGetValue("-AUTH_PASSWORD", out password);

            OnLoginWithUsername();
        }
        else
        {
            BytewarsLogger.Log("No auth credentials provided. Skipping auto login.");
        }
    }

    private void OnLoginWithUsername()
    {
        if (username == string.Empty || password == string.Empty)
        {
            OnLoginCompleted(Result<TokenData, OAuthError>.CreateError(new OAuthError() { error = "Email and Password fields cannot be empty." }));
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        OnRetryLoginClicked = OnLoginWithUsername;
        authWrapper.LoginWithUsername(username, password, OnLoginCompleted);
    }
    #endregion
}
