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

    private AuthEssentialsWrapper authEssentialsWrapper;

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
        
        if (authEssentialsWrapper == null)
        {
            authEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        }
        
        if (friendsEssentialsWrapper != null && authEssentialsWrapper != null)
        {
            LoadIncomingFriendRequests();
        }
    }

    private void Awake()
    {
        CurrentView = FriendRequestsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        FriendsEssentialsWrapper.OnIncomingRequest += OnIncomingFriendRequest;
        FriendsEssentialsWrapper.OnRequestCanceled += OnFriendRequestCanceled;
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
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(userId, result));
    }

    private void AcceptFriendInvitation(string userId)
    {
        friendsEssentialsWrapper.AcceptFriend(userId, result => OnInvitationResponseCompleted(userId, result));
    }

    private void DeclineFriendInvitation(string userId)
    {
        friendsEssentialsWrapper.DeclineFriend(userId, result => OnInvitationResponseCompleted(userId, result));
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
        
        CurrentView = FriendRequestsView.LoadSuccess;
        
        GetBulkUserInfo(result.Value);
    }

    private void OnGetBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (!result.IsError)
        {
            PopulateFriendRequestList(result.Value.data);
        }
    }

    private void OnGetAvatarCompleted(string userId, Result<Texture2D> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Unable to get avatar for user Id: {userId}, " +
                $"Error Code: {result.Error.Code}, " +
                $"Error Message: {result.Error.Message}");
            return;
        }

        if (result.Value is null || !friendRequests.TryGetValue(userId, out GameObject friendEntry))
        {
            return;
        }

        Image friendImage = friendEntry.GetComponent<FriendRequestsEntryHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }

    private void OnInvitationResponseCompleted(string userId, IResult result)
    {
        if (result.IsError)
        {
            return;
        }
        
        Destroy(friendRequests[userId]);
        
        if (friendRequests.ContainsKey(userId))
        {
            friendRequests.Remove(userId);
        }

        if (friendRequests.Count <= 0)
        {
            CurrentView = FriendRequestsView.Default;
        }
    }
    
    private void OnIncomingFriendRequest(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (CurrentView != FriendRequestsView.LoadSuccess)
        {
            CurrentView = FriendRequestsView.LoadSuccess;
        }

        authEssentialsWrapper.GetUserByUserId(userId, result =>
        {
            if (result.IsError)
            {
                return;
            }

            CreateFriendEntry(userId, result.Value.displayName);
        });
    }
    
    private void OnFriendRequestCanceled(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (friendRequests.ContainsKey(userId))
        {
            friendRequests.Remove(userId);
        }

        Destroy(friendRequests[userId]);

        if (friendRequests.Count <= 0)
        {
            CurrentView = FriendRequestsView.Default;
        }
    }

    #endregion Callback Functions

    #region View Management

    private void ClearFriendRequestList()
    {
        resultContentPanel.DestroyAllChildren();

        friendRequests.Clear();
    }

    private void PopulateFriendRequestList(params BaseUserInfo[] userInfo)
    {
        foreach (BaseUserInfo baseUserInfo in userInfo)
        {
            CreateFriendEntry(baseUserInfo.userId, baseUserInfo.displayName);
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
