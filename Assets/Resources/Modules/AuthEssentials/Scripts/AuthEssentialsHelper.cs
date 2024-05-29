// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;

public class AuthEssentialsHelper : MonoBehaviour
{    
    private AuthEssentialsWrapper authWrapper;
    public static Action OnUserLogout {get; private set;}

    private void Start()
    {
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        MainMenu.OnQuitPressed += OnQuitPressed;
        OnUserLogout += UserLogout;
    }

    private void OnQuitPressed(Action action)
    {
        authWrapper.Logout(action);
    }

    private void UserLogout()
    {
        authWrapper.Logout(BackToLoginMenu);
    }

    private static void BackToLoginMenu()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.LoginMenuCanvas);
    }
}
