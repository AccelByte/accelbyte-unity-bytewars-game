// Copyright (c) 2025 - 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Core;
using Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerListMenu : MenuCanvas
{
    [Header("Player Component")]
    [SerializeField] private GameObject playerEntryPrefab;
    
    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> playerEntries = new();

    private RecentPlayersWrapper recentPlayersWrapper;
    private AssetEnum friendDetailsAssetEnum;

    private void OnEnable()
    {
        if (recentPlayersWrapper == null)
        {
            recentPlayersWrapper = TutorialModuleManager.Instance.GetModuleClass<RecentPlayersWrapper>();
        }
        
        if (recentPlayersWrapper == null)
        {
            BytewarsLogger.LogWarning("recentPlayersWrapper is not enabled");
            return;
        }

        if (GameManager.Instance.InGamePause != null)
        {
            backButton.onClick.AddListener(GameManager.Instance.InGamePause.BackToPreviousPauseMenu);
        }
        
        LoadPlayerList();
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveAllListeners();
    }

    private void Awake()
    {
        friendDetailsAssetEnum = FriendsEssentialsModels.GetMenuByDependencyModule();
    }

    #region Player List Module
    
    private void LoadPlayerList()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        ClearEntries();
        
        recentPlayersWrapper.QueryPlayersListInCurrentSession(OnQueryPlayerListCompleted);
    }
    private void OnQueryPlayerListCompleted (Result<List<RecentPlayerInfo>> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning(
                "Error querying current session members, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
        
        foreach (RecentPlayerInfo playerInfo in result.Value)
        {
            CreatePlayerEntry(playerInfo);
        }
    }

    private void CreatePlayerEntry(RecentPlayerInfo playerInfo)
    {
        bool shouldPlaceOnRightPanel = resultColumnLeftPanel.childCount > resultColumnRightPanel.childCount;
        GameObject playerEntry = Instantiate(playerEntryPrefab, shouldPlaceOnRightPanel ? resultColumnRightPanel : resultColumnLeftPanel);
        playerEntry.name = playerInfo.UserId;

        RecentPlayerEntry recentPlayerEntry = playerEntry.GetComponent<RecentPlayerEntry>();
        recentPlayerEntry.Init(playerInfo.DisplayName, playerInfo.Availability.ToString(), playerInfo.SessionStatus.ToString(), playerInfo.AvatarUrl);

        if (GameData.CachedPlayerState.PlayerId != playerInfo.UserId)
        {
            Button friendButton = playerEntry.GetComponent<Button>();
            friendButton.onClick.AddListener(() => OnPlayerEntryClicked(playerInfo.UserId, playerInfo.DisplayName, recentPlayerEntry));
        }
    }
    private void OnPlayerEntryClicked(string userId, string name, RecentPlayerEntry entry)
    {
        if (!MenuManager.Instance.AllMenu.TryGetValue(AssetEnum.FriendDetailsMenu, out MenuCanvas menuCanvas))
        {
            BytewarsLogger.LogWarning($"Unable to find {friendDetailsAssetEnum} in menu manager");
            return;
        }
        
        if (menuCanvas.gameObject.TryGetComponent(out FriendDetailsMenu friendDetailsMenu))
        {
            friendDetailsMenu.UserId = userId;
            friendDetailsMenu.FriendImage.sprite = entry.GetCurrentImage();
            friendDetailsMenu.FriendDisplayName.text = name;
        }
        
        GameManager.Instance.InGamePause.ShowInGamePauseMenu(AssetEnum.FriendDetailsMenu);
    }
    
    #endregion Player List Module

    private void ClearEntries()
    {
        resultColumnLeftPanel.DestroyAllChildren();
        resultColumnRightPanel.DestroyAllChildren();
        playerEntries.Clear();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PlayerListMenu;
    }
}
