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

public class BlockedPlayersMenuHandler : MenuCanvas
{
    [Header("Blocked Players Component"), SerializeField] private GameObject playerEntryPrefab;
    
    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> blockedPlayers = new();
    
    private ManagingFriendsWrapper managingFriendsWrapper;

    private FriendsEssentialsWrapper friendEssentialsWrapper;
    
    private enum BlockedFriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private BlockedFriendsView currentView = BlockedFriendsView.Default;

    private BlockedFriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == BlockedFriendsView.Default);
            loadingPanel.gameObject.SetActive(value == BlockedFriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == BlockedFriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == BlockedFriendsView.LoadSuccess);
            currentView = value;
        }
    }
    
    private void OnEnable()
    {
        managingFriendsWrapper ??= TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        friendEssentialsWrapper ??= TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        
        if (managingFriendsWrapper != null && friendEssentialsWrapper != null)
        {
            LoadBlockedPlayers();
        }
    }
    
    private void Awake()
    {
        CurrentView = BlockedFriendsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }
    
    private void OnDisable()
    {
        ClearBlockedPlayers();

        CurrentView = BlockedFriendsView.Default;
    }
    
    #region Managing Friends Module

    #region Main Functions

    private void LoadBlockedPlayers()
    {
        CurrentView = BlockedFriendsView.Loading;

        managingFriendsWrapper.GetBlockedPlayers(OnLoadBlockedPlayersCompleted);
    }

    private void GetBulkUserInfo(params string[] userIds)
    {
        friendEssentialsWrapper.GetBulkUserInfo(userIds, OnGetBulkUserInfoCompleted);
    }

    private void UnblockPlayer(string userId)
    {
        managingFriendsWrapper.UnblockPlayer(userId, result => OnUnblockPlayerCompleted(userId, result));
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(userId, result));
    }
    
    #endregion Main Functions

    #region Callback Functions
    
    private void OnUnblockPlayerCompleted(string userId, Result<UnblockPlayerResponse> result)
    {
        if (result.IsError)
        {
            return;
        }
        
        if (!blockedPlayers.Remove(userId, out GameObject playerEntry))
        {
            return;
        }
        
        Destroy(playerEntry);
        
        if (blockedPlayers.Count <= 0)
        {
            CurrentView = BlockedFriendsView.Default;
        }
    }
    
    private void OnGetBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (result.IsError)
        {
            return;
        }

        CurrentView = BlockedFriendsView.LoadSuccess;

        ClearBlockedPlayers();

        PopulateBlockedPlayers(result.Value.data);
    }
    
    private void OnLoadBlockedPlayersCompleted(Result<BlockedList> result)
    {
        if (result.IsError)
        {
            CurrentView = BlockedFriendsView.LoadFailed;
            
            return;
        }

        BlockedData[] blockedData = result.Value.data;

        if (blockedData.Length <= 0)
        {
            CurrentView = BlockedFriendsView.Default;
            
            return;
        }
        
        IEnumerable<string> blockedPlayerIds = blockedData.Select(player => player.blockedUserId);

        GetBulkUserInfo(blockedPlayerIds.ToArray());
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

        if (!playerEntry.TryGetComponent(out BlockedPlayerEntryHandler playerEntryHandler))
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
    
    private void PopulateBlockedPlayers(params BaseUserInfo[] userInfo)
    {
        foreach (BaseUserInfo baseUserInfo in userInfo)
        {
            CreatePlayerEntry(baseUserInfo.userId, baseUserInfo.displayName);
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

        BlockedPlayerEntryHandler playerEntryHandler = playerEntry.GetComponent<BlockedPlayerEntryHandler>();
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
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BlockedPlayersMenuCanvas;
    }

    #endregion Menu Canvas Override
}
