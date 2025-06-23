// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendsMenu : MenuCanvas
{
    [Header("Friends Component")]
    [SerializeField] private GameObject friendEntryPrefab;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;
    [SerializeField] private Button backButton;
    
    private readonly Dictionary<string, GameObject> friendEntries = new();

    private AssetEnum friendDetailsAssetEnum;

    private FriendsEssentialsWrapper friendsEssentialsWrapper;
    
    private void OnEnable()
    {
        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }

        if (friendsEssentialsWrapper == null)
        {
            BytewarsLogger.LogWarning("FriendsEssentialsWrapper is not enabled");
            return;
        }

        LoadFriendList();
    }
    
    private void Awake()
    {
        friendDetailsAssetEnum = FriendsEssentialsModels.GetMenuByDependencyModule();
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        
        FriendsEssentialsModels.OnRequestAccepted += OnFriendListUpdate;
        ManagingFriendsModels.OnPlayerUnfriended += OnFriendListUpdate;
        ManagingFriendsModels.OnPlayerBlocked += OnFriendListUpdate;

        ClearFriendList();
    }

    private void OnDestroy()
    {
        FriendsEssentialsModels.OnRequestAccepted -= OnFriendListUpdate;
        ManagingFriendsModels.OnPlayerUnfriended -= OnFriendListUpdate;
        ManagingFriendsModels.OnPlayerBlocked -= OnFriendListUpdate;
    }
    
    private void OnDisable()
    {
        ClearFriendList();
    }

    #region Friend List Module

    #region Main Functions
    
    private void LoadFriendList()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        ClearFriendList();

        friendsEssentialsWrapper.GetFriendList(OnLoadFriendListCompleted);
    }
    
    private void GetBulkUserInfo(Friends friends)
    {
        friendsEssentialsWrapper.GetBulkUserInfo(friends.friendsId, OnGetBulkUserInfoCompleted);
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(result, userId));
    }

    #endregion Main Functions

    #region Callback Functions
    
    private void OnLoadFriendListCompleted(Result<Friends> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }
        
        if (result.Value.friendsId.Length <= 0)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
            return;
        }
        
        GetBulkUserInfo(result.Value);
    }

    private void OnGetBulkUserInfoCompleted(Result<AccountUserPlatformInfosResponse> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        ClearFriendList();
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);

        PopulateFriendList(result.Value.Data);
    }

    private void OnGetAvatarCompleted(Result<Texture2D> result, string userId)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Unable to get avatar for user Id: {userId}, " +
                $"Error Code: {result.Error.Code}, " +
                $"Error Message: {result.Error.Message}");
            return;
        }
        
        if (result.Value == null || !friendEntries.TryGetValue(userId, out GameObject friendEntry))
        {
            return;
        }
        
        Image friendImage = friendEntry.GetComponent<FriendEntry>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }
    
    private void OnFriendListUpdate(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        LoadFriendList();
    }

    #endregion Callback Functions

    #region View Management

    private void ClearFriendList()
    {
        resultColumnLeftPanel.DestroyAllChildren();
        resultColumnRightPanel.DestroyAllChildren();

        friendEntries.Clear();
    }
    
    private GameObject InstantiateToColumn(GameObject playerEntryPrefab)
    {
        bool shouldPlaceOnRightPanel = resultColumnLeftPanel.childCount > resultColumnRightPanel.childCount;
        
        return Instantiate(playerEntryPrefab, shouldPlaceOnRightPanel ? resultColumnRightPanel : resultColumnLeftPanel);
    }
    
    private void PopulateFriendList(params AccountUserPlatformData[] userInfo)
    {
        foreach (AccountUserPlatformData baseUserInfo in userInfo)
        {
            CreateFriendEntry(baseUserInfo.UserId, baseUserInfo.DisplayName);
        }
    }
    
    private void CreateFriendEntry(string userId, string displayName)
    {
        GameObject playerEntry = InstantiateToColumn(friendEntryPrefab);
        playerEntry.name = userId;
        
        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }
        
        FriendEntry playerEntryHandler = playerEntry.GetComponent<FriendEntry>();
        playerEntryHandler.EntryView = FriendEntry.FriendEntryView.Default;
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;

        Button friendButton = playerEntry.GetComponent<Button>();
        friendButton.onClick.AddListener(() => OnFriendEntryClicked(userId, displayName, playerEntryHandler));

        friendEntries.Add(userId, playerEntry);
        
        RetrieveUserAvatar(userId);
    }
    
    private void OnFriendEntryClicked(string userId, string displayName, FriendEntry playerEntryHandler)
    {
        if (friendDetailsAssetEnum is AssetEnum.FriendDetailsMenu)
        {
            MenuManager.Instance.InstantiateCanvas(friendDetailsAssetEnum);
        }
        
        if (!MenuManager.Instance.AllMenu.TryGetValue(friendDetailsAssetEnum, out MenuCanvas menuCanvas))
        {
            BytewarsLogger.LogWarning($"Unable to find {friendDetailsAssetEnum} in menu manager");
            return;
        }
        
        if (menuCanvas.gameObject.TryGetComponent(out FriendDetailsMenu friendDetailMenu))
        {
            friendDetailMenu.UserId = userId;
            friendDetailMenu.FriendImage.sprite = playerEntryHandler.FriendImage.sprite;
            friendDetailMenu.FriendDisplayName.text = displayName;
        }
        
        MenuManager.Instance.ChangeToMenu(friendDetailsAssetEnum);
    }
    
    #endregion View Management

    #endregion Friend List Module

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendsMenu;
    }

    #endregion Menu Canvas Override
}
