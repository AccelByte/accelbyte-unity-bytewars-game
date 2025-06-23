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

public class SentFriendRequestsMenu : MenuCanvas
{
    [Header("Friend Request Components")]
    [SerializeField] private GameObject friendEntryPrefab;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();

    private FriendsEssentialsWrapper friendsEssentialsWrapper;

    private void OnEnable()
    {
        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }

        if (friendsEssentialsWrapper != null)
        {
            LoadOutgoingFriendRequests();
        }
    }

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        FriendsEssentialsModels.OnRequestRejected += OnFriendRequestUpdated;
        FriendsEssentialsModels.OnRequestAccepted += OnFriendRequestUpdated;
    }
    
    private void OnDisable()
    {
        ClearFriendRequestList();
    }

    #region Add Friends Module

    #region Main Functions

    private void LoadOutgoingFriendRequests()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        friendsEssentialsWrapper.LoadOutgoingFriendRequests(OnLoadOutgoingRequestsCompleted);
    }

    private void GetBulkUserInfo(Friends friends)
    {
        friendsEssentialsWrapper.GetBulkUserInfo(friends.friendsId, OnGetBulkUserInfoCompleted);
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(result, userId));
    }

    private void CancelFriendRequest(string userId)
    {
        MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsEssentialsModels.CancelingFriendRequestMessage);

        friendsEssentialsWrapper.CancelFriendRequests(userId, result => OnCancelFriendRequestCompleted(userId, result));
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnLoadOutgoingRequestsCompleted(Result<Friends> result)
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
        
        ClearFriendRequestList();
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);

        PopulateFriendRequestList(result.Value.Data);
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
        
        if (result.Value == null || !friendRequests.TryGetValue(userId, out GameObject friendEntry))
        {
            return;
        }
        
        Image friendImage = friendEntry.GetComponent<FriendEntry>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }

    private void OnCancelFriendRequestCompleted(string userId, IResult result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        MenuManager.Instance.PromptMenu.ShowPromptMenu("Message", FriendsEssentialsModels.FriendRequestCanceledMessage, "OK", null);
        LoadOutgoingFriendRequests();
    }
    
    private void OnFriendRequestUpdated(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        LoadOutgoingFriendRequests();
    }

    #endregion Callback Functions

    #region View Management

    private void ClearFriendRequestList()
    {
        resultContentPanel.DestroyAllChildren();

        friendRequests.Clear();
    }

    private void PopulateFriendRequestList(params AccountUserPlatformData[] userInfo)
    {
        foreach (AccountUserPlatformData baseUserInfo in userInfo)
        {
            CreateFriendEntry(baseUserInfo.UserId, baseUserInfo.DisplayName);
        }
    }

    private void CreateFriendEntry(string userId, string displayName)
    {
        GameObject playerEntry = Instantiate(friendEntryPrefab, resultContentPanel);
        playerEntry.name = userId;

        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }

        FriendEntry playerEntryHandler = playerEntry.GetComponent<FriendEntry>();
        playerEntryHandler.EntryView = FriendEntry.FriendEntryView.PendingOutbound;
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;
        playerEntryHandler.CancelButton.onClick.AddListener(() => CancelFriendRequest(userId));

        friendRequests.Add(userId, playerEntry);

        RetrieveUserAvatar(userId);
    }

    #endregion View Management

    #endregion Add Friends Module

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SentFriendRequestsMenu;
    }
}
