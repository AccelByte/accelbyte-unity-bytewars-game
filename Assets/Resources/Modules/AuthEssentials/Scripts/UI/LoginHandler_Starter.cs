// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.Events;



#if UNITY_EDITOR
using ParrelSync;
#endif

public class LoginHandler_Starter : MenuCanvas
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

    //Declare each view panels
    [SerializeField] private GameObject loginStatePanel;
    [SerializeField] private GameObject loginLoadingPanel;
    [SerializeField] private GameObject loginFailedPanel;
    
    // Declare UI button here
    [SerializeField] private Button loginWithDeviceIdButton;
    [SerializeField] private Button retryLoginButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button loginWithSteamButton;
    [SerializeField] private TMP_Text failedMessageText;


    //TODO: Copy AuthEssentialsSubsystem_Starter here
    //TODO: Copy LoginType here

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
            loginStatePanel.SetActive(value == LoginView.LoginState);
            loginLoadingPanel.SetActive(value == LoginView.LoginLoading);
            loginFailedPanel.SetActive(value == LoginView.LoginFailed);
        }
    }

    #endregion

    //TODO: Copy OnEnable() here

    //TODO: Copy OnLoginWithDeviceIdButtonClicked()

    //TODO: Copy Start() here
    //TODO: then update it using code from "Put it All together"


    //TODO: Copy Login() function here
    //TODO: then change it using code from "Put it All together"


    //TODO: Copy all callback function from "Add a Login Menu" here
    //TODO: Then update OnLoginWithDeviceIdButtonClicked and OnRetryLoginButtonClicked from "Put it All together"

    private void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    
    //TODO: Copy OnLoginCompleted() from "Put it All together"


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
        return AssetEnum.LoginMenuCanvas_Starter;
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
