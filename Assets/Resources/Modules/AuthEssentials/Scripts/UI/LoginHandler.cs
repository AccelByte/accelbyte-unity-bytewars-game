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

#if UNITY_EDITOR
using ParrelSync;
#endif

public class LoginHandler : MenuCanvas
{
    [SerializeField] private GameObject loginStatePanel;
    [SerializeField] private GameObject loginLoadingPanel;
    [SerializeField] private GameObject loginFailedPanel;
    [SerializeField] private Button loginWithDeviceIdButton;
    [SerializeField] private Button retryLoginButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private TMP_Text failedMessageText;
    [SerializeField] private Button loginWithSteamButton;

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

    private AuthEssentialsWrapper _authWrapper;
    private LoginType _lastLoginMethod;
        
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
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        loginWithDeviceIdButton.onClick.AddListener(OnLoginWithDeviceIdButtonClicked);
        retryLoginButton.onClick.AddListener(OnRetryLoginButtonClicked);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);
    }

    private void OnEnable()
    {
        CurrentView = LoginView.LoginState;
    }

    private void Login(LoginType loginMethod)
    {
        CurrentView = LoginView.LoginLoading;
        _lastLoginMethod = loginMethod;
        OnRetryLoginClicked = OnRetryLoginButtonClicked;
        _authWrapper.Login(loginMethod, OnLoginCompleted);
    }

    public void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (!result.IsError)
        {
            onLoginCompleted.Invoke(result.Value);
            MenuManager.Instance.ChangeToMenu(AssetEnum.MainMenuCanvas);
            Debug.Log($"[LoginHandler.OnLoginCompleted] success: {result.Value.ToJsonString()}");
        }
        else
        {
            failedMessageText.text = "Login Failed: "  + result.Error.error;
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
        string[] cmdArgs = Environment.GetCommandLineArgs();

#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            cmdArgs = ClonesManager.GetArgument().Split();
        }
#endif
        string username = "";
        string password = "";

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
        if (username != "" && password != "")
        {
            _authWrapper.LoginWithUsername(username, password, OnLoginCompleted);
        }
        
    }
    
    private void OnLoginWithDeviceIdButtonClicked()
    {
        bool argCheckingLoginWithUsername = Environment.GetCommandLineArgs().Contains("-AUTH_TYPE=ACCELBYTE");

#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            argCheckingLoginWithUsername = ClonesManager.GetArgument().Contains("-AUTH_TYPE=ACCELBYTE");
        }
#endif
        if (argCheckingLoginWithUsername)
        {
            AutoLoginCmd();
        }
        else
        {
            Login(LoginType.DeviceId);
        }
    }

    private void OnRetryLoginButtonClicked()
    {
        Login(_lastLoginMethod);
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
