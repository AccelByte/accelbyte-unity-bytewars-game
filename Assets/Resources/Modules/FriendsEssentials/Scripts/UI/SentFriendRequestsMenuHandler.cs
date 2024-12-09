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

public class SentFriendRequestsMenuHandler : MenuCanvas
{
    [Header("Friend Request Components"), SerializeField] private GameObject friendEntryPrefab;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();

    private FriendsEssentialsWrapper friendsEssentialsWrapper;

    private enum SentFriendRequestsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private SentFriendRequestsView currentView = SentFriendRequestsView.Default;

    private SentFriendRequestsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == SentFriendRequestsView.Default);
            loadingPanel.gameObject.SetActive(value == SentFriendRequestsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == SentFriendRequestsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == SentFriendRequestsView.LoadSuccess);
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
            LoadOutgoingFriendRequests();
        }
    }

    private void Awake()
    {
        CurrentView = SentFriendRequestsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        FriendsEssentialsWrapper.OnRequestRejected += OnFriendRequestUpdated;
        FriendsEssentialsWrapper.OnRequestAccepted += OnFriendRequestUpdated;
    }
    
    private void OnDisable()
    {
        ClearFriendRequestList();

        CurrentView = SentFriendRequestsView.Default;
    }

    #region Add Friends Module

    #region Main Functions

    private void LoadOutgoingFriendRequests()
    {
        CurrentView = SentFriendRequestsView.Loading;

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
        MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.CancelingFriendRequestMessage);

        friendsEssentialsWrapper.CancelFriendRequests(userId, result => OnCancelFriendRequestCompleted(userId, result));
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnLoadOutgoingRequestsCompleted(Result<Friends> result)
    {
        if (result.IsError)
        {
            CurrentView = SentFriendRequestsView.LoadFailed;
            return;
        }
        
        if (result.Value.friendsId.Length <= 0)
        {
            CurrentView = SentFriendRequestsView.Default;
            return;
        }

        GetBulkUserInfo(result.Value);
    }

    private void OnGetBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (result.IsError)
        {
            CurrentView = SentFriendRequestsView.LoadFailed;
            return;
        }
        
        ClearFriendRequestList();
        CurrentView = SentFriendRequestsView.LoadSuccess;

        PopulateFriendRequestList(result.Value.data);
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
        
        Image friendImage = friendEntry.GetComponent<SentFriendRequestsEntryHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }

    private void OnCancelFriendRequestCompleted(string userId, IResult result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        MenuManager.Instance.PromptMenu.ShowPromptMenu("Message", FriendsHelper.FriendRequestCanceledMessage, "OK", null);
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

        SentFriendRequestsEntryHandler playerEntryHandler = playerEntry.GetComponent<SentFriendRequestsEntryHandler>();
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
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SentFriendRequestsMenuCanvas;
    }
}
