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

public class LoginMenu_Starter : MenuCanvas
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

    // TODO: Declare tutorial module variables here.

    private void Start()
    {
        quitGameButton.onClick.AddListener(OnQuitGameButtonClicked);

        // TODO: Add the tutorial module code here.
    }

    private void OnEnable()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
    }

    private void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
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
        return AssetEnum.LoginMenu_Starter;
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

    // TODO: Declare the tutorial module functions here
}
