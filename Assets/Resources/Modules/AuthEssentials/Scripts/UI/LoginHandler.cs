// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class LoginHandler : MenuCanvas
{
    public delegate void LoginHandlerDelegate(TokenData tokenData);
    public static event LoginHandlerDelegate onLoginCompleted = delegate {};
    public UnityAction OnRetryLoginClicked
    {
        set
        {
            retryLoginButton.onClick.RemoveAllListeners();
            retryLoginButton.onClick.AddListener(value);
        }
    }
    [SerializeField] private GameObject loginStatePanel;
    [SerializeField] private GameObject loginLoadingPanel;
    [SerializeField] private GameObject loginFailedPanel;
    [SerializeField] private Button loginWithDeviceIdButton;
    [SerializeField] private Button retryLoginButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private TMP_Text failedMessageText;
    [SerializeField] private Button loginWithSteamButton;
    private AuthEssentialsWrapper authWrapper;
    private LoginType lastLoginMethod;
        
    #region LoginView enum
    public enum LoginView
    {
        LoginState,
        LoginLoading,
        LoginFailed
    }
    
    private LoginView CurrentView
    {
        get => CurrentView;
        set
        {
            switch (value)
            {
                case LoginView.LoginState:
                    loginStatePanel.SetActive(true);
                    loginLoadingPanel.SetActive(false);
                    loginFailedPanel.SetActive(false);
                    break;

                case LoginView.LoginLoading:
                    loginStatePanel.SetActive(false);
                    loginLoadingPanel.SetActive(true);
                    loginFailedPanel.SetActive(false);
                    break;
                
                case LoginView.LoginFailed:
                    loginStatePanel.SetActive(false);
                    loginLoadingPanel.SetActive(false);
                    loginFailedPanel.SetActive(true);
                    break;
            }
        }
    }

    #endregion
    
    private void Start()
    {
        // get auth's subsystem
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        loginWithDeviceIdButton.onClick.AddListener(OnLoginWithDeviceIdButtonClicked);
        retryLoginButton.onClick.AddListener(OnRetryLoginButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);
    }

    private void OnEnable()
    {
        StartCoroutine(CheckWrapper(AutoLogin));
        CurrentView = LoginView.LoginState;
    }

    private void AutoLogin()
    {
        if ( MenuManager.Instance.GetCurrentMenu() is LoginHandler && !authWrapper.GetActiveUser())
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AutoLoginURL();
#else
            AutoLoginCmd();
#endif
        }
    }

    private void OnDisable()
    {
        StopCoroutine(CheckWrapper(AutoLogin));
    }

    IEnumerator CheckWrapper(Action action)
    {
        while (authWrapper == null)
        {
            yield return null; // Wait for the next frame
        }

        action.Invoke();
    }

    private void Login(LoginType loginMethod)
    {
        CurrentView = LoginView.LoginLoading;
        lastLoginMethod = loginMethod;
        OnRetryLoginClicked = OnRetryLoginButtonClicked;
        authWrapper.LoginWithDeviceId(OnLoginCompleted);
    }

    public void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (!result.IsError)
        {
            onLoginCompleted.Invoke(result.Value);
            MenuManager.Instance.ChangeToMenu(AssetEnum.MainMenuCanvas);
            BytewarsLogger.Log($"[LoginHandler.OnLoginCompleted] success: {result.Value.ToJsonString()}");
        }
        else
        {
            failedMessageText.text = $"Login Failed: {result.Error.error}";
            CurrentView = LoginView.LoginFailed;
            StartCoroutine(SetSelectedGameObject(retryLoginButton.gameObject));
        }
    }

    IEnumerator SetSelectedGameObject(GameObject gameObjectToSelect)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(gameObjectToSelect);
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

        string username = string.Empty;
        string password = string.Empty;

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

        // try login with the username and password specified with command-line arguments
        if (username != string.Empty && password != string.Empty)
        {
            CurrentView = LoginView.LoginLoading;
            authWrapper.LoginWithUsername(username, password, OnLoginCompleted);
        }
        else
        {
            failedMessageText.text = $"Login Failed:\n Email and Password fields cannot be empty.";
            failedMessageText.alignment = TextAlignmentOptions.Center;
            CurrentView = LoginView.LoginFailed;
            StartCoroutine(SetSelectedGameObject(retryLoginButton.gameObject));
            retryLoginButton.gameObject.SetActive(false);
        }
    }
    
    private void OnLoginWithDeviceIdButtonClicked()
    {
        Login(LoginType.DeviceId);
    }

    private void AutoLoginURL()
    {
        Dictionary<string, string> urlParams = ConnectionHandler.GetURLParameters();

        if (urlParams.Count == 0)
        {
            BytewarsLogger.Log($"Url Params Not found");
            return;
        }

        CurrentView = LoginView.LoginLoading;

        if (urlParams.TryGetValue("-AUTH_TYPE", out string authType))
        {
            if (authType == "ACCELBYTE")
            {
                urlParams.TryGetValue("-AUTH_LOGIN", out string email);
                urlParams.TryGetValue("-AUTH_PASSWORD", out string password);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    failedMessageText.text = $"Login Failed:\n Email and Password fields cannot be empty.";
                    CurrentView = LoginView.LoginFailed;
                    StartCoroutine(SetSelectedGameObject(retryLoginButton.gameObject));
                    retryLoginButton.gameObject.SetActive(false);
                    return;
                }

                authWrapper.LoginWithUsername(email, password, OnLoginCompleted);
            }
        }
    }

    private void OnRetryLoginButtonClicked()
    {
        Login(lastLoginMethod);
    }
    
    private void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public override GameObject GetFirstButton()
    {
        return loginWithDeviceIdButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LoginMenuCanvas;
    }
    
    public Button GetLoginButton(LoginType loginType)
    {
        switch (loginType)
        {
            case LoginType.Steam:
                return loginWithSteamButton;
            case LoginType.DeviceId:
                return loginWithDeviceIdButton;
        }
        return null;
    }

    public void SetView(LoginView loginView)
    {
        CurrentView = loginView;
    }
}
