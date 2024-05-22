// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FindFriendsMenuHandler : MenuCanvas
{
    [Header("Find Friends Components"), SerializeField] private GameObject friendEntryPrefab;
    [SerializeField] private TMP_Text friendCode;
    [SerializeField] private Button friendCodeCopyButton;
    [SerializeField] private TMP_InputField friendSearchBar;
    
    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;
    
    private const string FriendCodeCopiedMessage = "Copied!";
    private const string FriendCodePreloadMessage = "...";
    
    private GameObject userResult;
    
    private FriendEssentialsWrapper friendEssentialsWrapper;

    private readonly Dictionary<RelationshipStatusCode, (string, bool)> statusMessageButtonStateMap = new()
    {
        { RelationshipStatusCode.Friend, ("Already Friends", false) },
        { RelationshipStatusCode.Outgoing, ("Request Pending", false) },
        { RelationshipStatusCode.Incoming, ("Awaiting Response", false) },
        { RelationshipStatusCode.NotFriend, ("Not Friends", true) },
    };

    private enum FindFriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private FindFriendsView currentView = FindFriendsView.Default;
    
    private FindFriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FindFriendsView.Default);
            loadingPanel.gameObject.SetActive(value == FindFriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FindFriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FindFriendsView.LoadSuccess);
            currentView = value;
        }
    }
    
    private void Awake()
    {
        CurrentView = FindFriendsView.Default;
        
        backButton.onClick.AddListener(OnBackButtonClicked);
        friendCodeCopyButton.onClick.AddListener(OnFriendCodeCopyButtonClicked);
        friendSearchBar.onSubmit.AddListener(FindFriend);
    }
    
    private void Start()
    {
        friendEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendEssentialsWrapper>();
        SetFriendCodeText(friendEssentialsWrapper.PlayerFriendCode);
    }
    
    private void OnDisable()
    {
        ClearSearchPanel();
        
        CurrentView = FindFriendsView.Default;
    }
    
    #region Button Callbacks
    
    private static void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    private async void OnFriendCodeCopyButtonClicked()
    {
        GUIUtility.systemCopyBuffer = friendCode.text;
        TMP_Text buttonText = friendCodeCopyButton.GetComponentInChildren<TMP_Text>();
        
        string originalText = buttonText.text;
        buttonText.SetText(FriendCodeCopiedMessage);
        friendCodeCopyButton.interactable = false;
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        buttonText.SetText(originalText);
        friendCodeCopyButton.interactable = true;
    }

    #endregion Button Callbacks

    #region Search for Players Module

    #region Main Functions

    /// <summary>
    /// Find user/friend
    /// </summary>
    /// <param name="query">Target user query</param>
    private void FindFriend(string query)
    {
        if (string.IsNullOrEmpty(friendSearchBar.text) || string.IsNullOrEmpty(friendSearchBar.text))
        {
            return;
        }

        CurrentView = FindFriendsView.Loading;
        ClearSearchPanel();

        friendEssentialsWrapper.GetUserByFriendCode(query, result =>
        {
            OnUsersFriendCodeFound(result, fallbackAction: () =>
            {
                BytewarsLogger.Log("Friend code not found, searching by exact display name");
                
                friendEssentialsWrapper.GetUserByExactDisplayName(query, OnUsersDisplayNameFound);
            });
        });
    }

    /// <summary>
    /// Send friend invitation
    /// </summary>
    /// <param name="userId">Target user id</param>
    private void SendFriendInvitation(string userId)
    {
        friendEssentialsWrapper.SendFriendRequest(userId, OnSendRequestComplete);
    }
    
    /// <summary>
    /// Get user avatar
    /// </summary>
    /// <param name="userId">Target user id</param>
    private void RetrievedUserAvatar(string userId)
    {
        friendEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarComplete(result, userId));
    }
    
    #endregion Main Functions
    
    #region Callback Functions
    
    /// <summary>
    /// Find friend with display name's callback
    /// </summary>
    /// <param name="result">Result of the display name search</param>
    private void OnUsersDisplayNameFound(Result<PublicUserInfo> result)
    {
        if (result.IsError)
        {
            CurrentView = FindFriendsView.LoadFailed;
            return;
        }
        
        CurrentView = FindFriendsView.LoadSuccess;
        
        GenerateFriendResult(result.Value.userId, result.Value.displayName);
        return;
    }
    
    /// <summary>
    /// Find friend with friend code's callback
    /// </summary>
    /// <param name="result">Result of the friend code search</param>
    /// <param name="fallbackAction">Fallback action if friend code search failed</param>
    private void OnUsersFriendCodeFound(Result<PublicUserData> result, Action fallbackAction = null)
    {
        if (result.IsError)
        {
            if (fallbackAction is not null)
            {
                fallbackAction.Invoke();
                return;
            }
            
            CurrentView = FindFriendsView.LoadFailed;
            return;
        }
        
        PublicUserData userData = result.Value;
        if (userData.userId == friendEssentialsWrapper.PlayerUserId)
        {
            BytewarsLogger.Log("Found friend code with self entry");
            
            CurrentView = FindFriendsView.Default;
            return;
        }
        
        CurrentView = FindFriendsView.LoadSuccess;
        GenerateFriendResult(userData.userId, userData.displayName);
    }
    
    /// <summary>
    /// SendFriendInvitation's callback
    /// </summary>
    /// <param name="result">Result of the friend request</param>
    private void OnSendRequestComplete(IResult result)
    {
        if (result.IsError || userResult is null)
        {
            return;
        }
        
        FindFriendsEntryHandler entryHandler = userResult.GetComponent<FindFriendsEntryHandler>();
        if (entryHandler is null)
        {
            return;
        }
        
        entryHandler.SendInviteButton.interactable = false;
        entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = "Request Sent";
    }
    
    private void OnGetFriendshipStatusCompleted(string userId, Result<FriendshipStatus> result)
    {
        if (userResult is null)
        {
            return;
        }
        
        FindFriendsEntryHandler entryHandler = userResult.GetComponent<FindFriendsEntryHandler>();
        if (result.IsError)
        {
            entryHandler.FriendStatus.text = "Unable to get friendship status";
            BytewarsLogger.LogWarning($"Unable to get friendship status: {result.Error.Message}");
            return;
        }
        
        RelationshipStatusCode friendshipStatus = result.Value.friendshipStatus;
        
        (string statusMessage, bool buttonState) = statusMessageButtonStateMap[friendshipStatus];
        entryHandler.FriendStatus.text = statusMessage;
        entryHandler.SendInviteButton.interactable = buttonState;
        
        if (friendshipStatus is RelationshipStatusCode.Outgoing)
        {
            entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = "Request Sent";
        }
    }
    
    /// <summary>
    /// RetrievedUserAvatar's callback
    /// </summary>
    /// <param name="result">Result of the avatar search</param>
    /// <param name="userId">Target user id</param>
    private void OnGetAvatarComplete(Result<Texture2D> result, string userId)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            return;
        }
        
        if (result.Value == null || userResult is null)
        {
            return;
        }
        
        Image entryImage = userResult.GetComponent<FindFriendsEntryHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        entryImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }
    
    #endregion Callback Functions
    
    #region View Management
    
    /// <summary>
    /// Clear results in search panel
    /// </summary>
    private void ClearSearchPanel()
    {
        friendSearchBar.text = string.Empty;

        resultContentPanel.DestroyAllChildren();

        if (userResult != null)
        {
            Destroy(userResult);
        }
    }
    
    private void GenerateFriendResult(string userId, string displayName)
    {
        if (userId == friendEssentialsWrapper.PlayerUserId)
        {
            BytewarsLogger.Log("Skipping self entry");
            return;
        }
        
        GameObject playerEntry = Instantiate(friendEntryPrefab, resultContentPanel);
        playerEntry.name = userId;
        
        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }
        
        FindFriendsEntryHandler playerEntryHandler = playerEntry.GetComponent<FindFriendsEntryHandler>();
        playerEntryHandler.FriendName.text = displayName;
        playerEntryHandler.SendInviteButton.onClick.AddListener(() => SendFriendInvitation(userId));
        
        userResult = playerEntry;
        
        CheckFriendshipStatus(userId);
        RetrievedUserAvatar(userId);
    }
    
    /// <summary>
    /// Helper function to get friendship status and Outgoing friend requests.
    /// </summary>
    /// <param name="userId">Target user id</param>
    private void CheckFriendshipStatus(string userId)
    {
        friendEssentialsWrapper.GetFriendshipStatus(userId, result => OnGetFriendshipStatusCompleted(userId, result));
    }
    
    private void SetFriendCodeText(string friendCodeString)
    {
        if (string.IsNullOrEmpty(friendCodeString))
        {
            friendCode.SetText(FriendCodePreloadMessage);
            return;
        }
        
        friendCode.SetText(friendCodeString);
    }

    #endregion View Management

    #endregion Search for Players Module

    #region MenuCanvas Overrides

    public override GameObject GetFirstButton()
    {
        return friendSearchBar.gameObject;
    }
    
    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FindFriendsMenuCanvas;
    }
    
    #endregion MenuCanvas Overrides
}
