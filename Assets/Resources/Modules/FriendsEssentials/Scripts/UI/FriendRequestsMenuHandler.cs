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

public class FriendRequestsMenuHandler : MenuCanvas
{
    [Header("Friend Request Components"), SerializeField] private GameObject friendEntryPrefab;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();
    
    private FriendsEssentialsWrapper friendsEssentialsWrapper;

    private enum FriendRequestsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private FriendRequestsView currentView = FriendRequestsView.Default;

    private FriendRequestsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FriendRequestsView.Default);
            loadingPanel.gameObject.SetActive(value == FriendRequestsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FriendRequestsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FriendRequestsView.LoadSuccess);
            currentView = value;
        }
    }

    private void OnEnable()
    {
        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }
        
        if (friendsEssentialsWrapper != null)
        {
            LoadIncomingFriendRequests();
        }
    }

    private void Awake()
    {
        CurrentView = FriendRequestsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        FriendsEssentialsWrapper.OnIncomingRequest += OnFriendRequestUpdated;
        FriendsEssentialsWrapper.OnRequestCanceled += OnFriendRequestUpdated;
    }

    private void OnDisable()
    {
        ClearFriendRequestList();
    }

    #region Add Friends Module

    #region Main Functions

    private void LoadIncomingFriendRequests()
    {
        CurrentView = FriendRequestsView.Loading;

        friendsEssentialsWrapper.LoadIncomingFriendRequests(OnLoadIncomingFriendRequestsCompleted);
    }

    private void GetBulkUserInfo(Friends friends)
    {
        friendsEssentialsWrapper.GetBulkUserInfo(friends.friendsId, OnGetBulkUserInfoCompleted);
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(result, userId));
    }

    private void AcceptFriendInvitation(string userId)
    {
        MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.AcceptingFriendRequestMessage);

        friendsEssentialsWrapper.AcceptFriend(userId, OnAcceptInvitationCompleted);
    }

    private void DeclineFriendInvitation(string userId)
    {
        MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.RejectingFriendRequestMessage);

        friendsEssentialsWrapper.DeclineFriend(userId, OnDeclineInvitationCompleted);
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnLoadIncomingFriendRequestsCompleted(Result<Friends> result)
    {
        if (result.IsError)
        {
            CurrentView = FriendRequestsView.LoadFailed;
            return;
        }
        
        if (result.Value.friendsId.Length <= 0)
        {
            CurrentView = FriendRequestsView.Default;
            return;
        }

        GetBulkUserInfo(result.Value);
    }

    private void OnGetBulkUserInfoCompleted(Result<AccountUserPlatformInfosResponse> result)
    {
        if (result.IsError)
        {
            CurrentView = FriendRequestsView.LoadFailed;
            return;
        }
        
        ClearFriendRequestList();
        CurrentView = FriendRequestsView.LoadSuccess;

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

        Image friendImage = friendEntry.GetComponent<FriendRequestsEntryHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }

    private void OnAcceptInvitationCompleted(IResult result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptMessageTitle,
            FriendsHelper.FriendRequestAcceptedMessage, "OK", null);

        LoadIncomingFriendRequests();
    }

    private void OnDeclineInvitationCompleted(IResult result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptMessageTitle,
            FriendsHelper.FriendRequestRejectedMessage, "OK", null);

        LoadIncomingFriendRequests();
    }
    
    private void OnFriendRequestUpdated(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        LoadIncomingFriendRequests();
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

        FriendRequestsEntryHandler playerEntryHandler = playerEntry.GetComponent<FriendRequestsEntryHandler>();
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;
        playerEntryHandler.AcceptButton.onClick.AddListener(() => AcceptFriendInvitation(userId));
        playerEntryHandler.RejectButton.onClick.AddListener(() => DeclineFriendInvitation(userId));

        friendRequests.Add(userId, playerEntry);

        RetrieveUserAvatar(userId);
    }

    #endregion View Management

    #endregion Add Friends Module

    public override GameObject GetFirstButton()
    {
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendRequestsMenuCanvas;
    }
}
