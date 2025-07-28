// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InviteFriendsButton : MonoBehaviour
{
    private Button button;
    private ModuleModel friendModule;

    private void Awake()
    {
        button = GetComponent<Button>();
        friendModule = TutorialModuleManager.Instance.GetModule(TutorialType.FriendsEssentials);
    }

    private void OnEnable()
    {
        // Only show button if the current session is a "Create Match" session.
        InGameMode gameMode =
            AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(AccelByteWarsOnlineSession.CachedSession);
        bool isMatchSession = gameMode == InGameMode.CreateMatchElimination || gameMode == InGameMode.CreateMatchTeamDeathmatch;
        gameObject.transform.localScale = isMatchSession ? Vector3.one : Vector3.zero;
        button.interactable = isMatchSession;

        // Skip button binding if the current match is not a match session.
        if (!isMatchSession)
        {
            return;   
        }
        
        button?.onClick?.AddListener(OpenFriendMenu);
    }

    private void OnDisable()
    {
        button?.onClick?.RemoveAllListeners();
    }

    private void OpenFriendMenu()
    {
        MenuManager.Instance.ChangeToMenu(friendModule.isStarterActive ? AssetEnum.FriendsMenu_Starter : AssetEnum.FriendsMenu);
    }
}
