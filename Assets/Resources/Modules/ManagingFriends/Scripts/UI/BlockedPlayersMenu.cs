// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class BlockedPlayersMenu : MenuCanvas
{
    [Header("Blocked Players Component")]
    [SerializeField] private GameObject playerEntryPrefab;
    
    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> blockedPlayers = new();
    
    private ManagingFriendsWrapper managingFriendsWrapper;

    private FriendsEssentialsWrapper friendsEssentialsWrapper;
    
    private void OnEnable()
    {
        if (managingFriendsWrapper == null)
        {
            managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        }

        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }
        
        if (managingFriendsWrapper != null && friendsEssentialsWrapper != null)
        {
            LoadBlockedPlayers();
        }
    }
    
    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }
    
    private void OnDisable()
    {
        ClearBlockedPlayers();
    }
    
    #region Managing Friends Module

    #region Main Functions

    private void LoadBlockedPlayers()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        managingFriendsWrapper.GetBlockedPlayers(OnLoadBlockedPlayersCompleted);
    }

    private void GetBulkUserInfo(params string[] userIds)
    {
        friendsEssentialsWrapper.GetBulkUserInfo(userIds, OnGetBulkUserInfoCompleted);
    }

    private void UnblockPlayer(string userId)
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptConfirmTitle,
            FriendsEssentialsModels.UnblockPlayerConfirmationMessage, 
            "Yes", 
            confirmAction: () => 
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsEssentialsModels.UnblockingPlayerMessage);

                managingFriendsWrapper.UnblockPlayer(userId, result => OnUnblockPlayerCompleted(userId, result));
            },
            "No", null);
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(userId, result));
    }
    
    #endregion Main Functions

    #region Callback Functions
    
    private void OnLoadBlockedPlayersCompleted(Result<BlockedList> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);

            return;
        }

        BlockedData[] blockedData = result.Value.data;

        if (blockedData.Length <= 0)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);

            return;
        }
        
        IEnumerable<string> blockedPlayerIds = blockedData.Select(player => player.blockedUserId);

        GetBulkUserInfo(blockedPlayerIds.ToArray());
    }
    
    private void OnGetBulkUserInfoCompleted(Result<AccountUserPlatformInfosResponse> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);

        ClearBlockedPlayers();

        PopulateBlockedPlayers(result.Value.Data);
    }
    
    private void OnUnblockPlayerCompleted(string userId, Result<UnblockPlayerResponse> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptMessageTitle,
            FriendsEssentialsModels.UnblockPlayerCompletedMessage, "OK", null);

        LoadBlockedPlayers();
    }
    
    private void OnGetAvatarCompleted(string userId, Result<Texture2D> result)
    {
        if (result.IsError)
        {
            return;
        }
        
        if (!blockedPlayers.TryGetValue(userId, out GameObject playerEntry))
        {
            return;
        }

        if (!playerEntry.TryGetComponent(out BlockedPlayerEntry playerEntryHandler))
        {
            return;
        }

        Rect rect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        playerEntryHandler.FriendImage.sprite = Sprite.Create(result.Value, rect, Vector2.zero);
    }

    #endregion Callback Functions

    #region View Management

    private void ClearBlockedPlayers()
    {
        resultContentPanel.DestroyAllChildren();
        
        blockedPlayers.Clear();
    }
    
    private void PopulateBlockedPlayers(params AccountUserPlatformData[] userInfo)
    {
        foreach (AccountUserPlatformData baseUserInfo in userInfo)
        {
            CreatePlayerEntry(baseUserInfo.UserId, baseUserInfo.DisplayName);
        }
    }
    
    private void CreatePlayerEntry(string userId, string displayName)
    {
        GameObject playerEntry = Instantiate(playerEntryPrefab, resultContentPanel);
        playerEntry.name = userId;

        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }

        BlockedPlayerEntry playerEntryHandler = playerEntry.GetComponent<BlockedPlayerEntry>();
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;
        playerEntryHandler.UnblockButton.onClick.AddListener(() => UnblockPlayer(userId));

        blockedPlayers.Add(userId, playerEntry);

        RetrieveUserAvatar(userId);
    }

    #endregion View Management

    #endregion

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BlockedPlayersMenu;
    }

    #endregion Menu Canvas Override
}
