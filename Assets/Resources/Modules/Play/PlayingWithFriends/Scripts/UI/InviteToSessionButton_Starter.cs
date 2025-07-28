// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class InviteToSessionButton_Starter : MonoBehaviour
{
    private Button button;
    private PlayingWithFriendsWrapper_Starter playingWithFriendswrapper;
    private string targetPlayerId = "";

#region UI Handling

    private void OnEnable()
    {
        // Retrieve target player's ID.
        FriendDetailsMenu friendDetailsMenu = MenuManager.Instance.GetCurrentMenu() as FriendDetailsMenu;
        FriendDetailsMenu_Starter friendDetailsMenu_Starter = MenuManager.Instance.GetCurrentMenu() as FriendDetailsMenu_Starter;
        if (friendDetailsMenu != null)
        {
            targetPlayerId = friendDetailsMenu.UserId;
        }
        else if (friendDetailsMenu_Starter != null)
        {
            targetPlayerId = friendDetailsMenu_Starter.UserId;
        }

        if (targetPlayerId.IsNullOrEmpty())
        {
            BytewarsLogger.LogWarning("Current menu is not friend details or friend details starter menu.");
            SetButtonVisibility(false);
            return;
        }
        
        // Only show if currently in game session and not in current game session.
        InGameMode gameMode =
            AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(AccelByteWarsOnlineSession.CachedSession);
        bool isMatchSession = gameMode == InGameMode.CreateMatchElimination || gameMode == InGameMode.CreateMatchTeamDeathmatch;
        bool isInMatch = GameManager.Instance.ConnectedPlayerStates.Any(x => x.Value.PlayerId == targetPlayerId);
        bool shouldVisible = isMatchSession && !isInMatch;
        SetButtonVisibility(shouldVisible);
        
        // Skip button binding if the button is set to hidden.
        if (!shouldVisible)
        {
            return;
        }

        button?.onClick?.AddListener(InviteToSession);
    }

    private void OnDisable()
    {
        button?.onClick.RemoveAllListeners();
    }

    private void SetButtonVisibility(bool shouldVisible)
    {
        gameObject.transform.localScale = shouldVisible ? Vector3.one : Vector3.zero;
        button.interactable = shouldVisible;
    }

#endregion

    private void Awake()
    {
        playingWithFriendswrapper = TutorialModuleManager.Instance.GetModuleClass<PlayingWithFriendsWrapper_Starter>();
        button = GetComponent<Button>();
    }
    
    private void InviteToSession()
    {
#region Tutorial
        // Put your code here.
#endregion
    }
}
