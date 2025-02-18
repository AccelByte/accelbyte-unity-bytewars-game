// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using AccelByte.Api;
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
    [SerializeField] private Button friendSearchButton;
    
    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;
    
    private GameObject userResult;
    
    private FriendsEssentialsWrapper friendsEssentialsWrapper;

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
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        friendCodeCopyButton.onClick.AddListener(OnFriendCodeCopyButtonClicked);
        friendSearchBar.onSubmit.AddListener(FindFriend);
        friendSearchButton.onClick.AddListener(() => FindFriend(friendSearchBar.text));
    }
    
    private void Start()
    {
        friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();

        SetFriendCodeText(friendsEssentialsWrapper.PlayerFriendCode);
    }
    
    private void OnDisable()
    {
        ClearSearchPanel();
        friendSearchBar.text = string.Empty;

        CurrentView = FindFriendsView.Default;
    }

    private void OnEnable()
    {
        friendSearchBar.enabled = true;
    }

    #region Search for Players Module

    #region Main Functions

    private void FindFriend(string query)
    {
        if (string.IsNullOrEmpty(friendSearchBar.text) || string.IsNullOrEmpty(friendSearchBar.text))
        {
            return;
        }

        CurrentView = FindFriendsView.Loading;
        friendSearchBar.enabled = false;
        ClearSearchPanel();

        // Find friend by friend code, make sure the friend code is in uppercase.
        friendsEssentialsWrapper.GetUserByFriendCode(query.ToUpper(), result =>
        {
            OnUsersFriendCodeFound(result, query, fallbackAction: () =>
            {
                BytewarsLogger.Log("Friend code not found, searching by exact display name");
                
                friendsEssentialsWrapper.GetUserByExactDisplayName(query, result => OnUsersDisplayNameFound(result, query));
            });
        });
    }

    private void SendFriendInvitation(string userId, bool usingFriendCode = false)
    {
        if (!usingFriendCode)
        {
            MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.SendingFriendRequestMessage);
        }

        friendsEssentialsWrapper.SendFriendRequest(userId, result => OnSendRequestComplete(result, usingFriendCode));
    }
    
    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(result, userId));
    }
    
    #endregion Main Functions
    
    #region Callback Functions
    
    private void OnUsersDisplayNameFound(Result<PublicUserInfo> result, string query)
    {
        if (result.IsError)
        {
            CurrentView = FindFriendsView.LoadFailed;
            string queryNotFoundMessage = FriendsHelper.QueryNotFoundMessage.Replace("%QUERY%", query);
            loadingFailedPanel.GetComponentInChildren<TMP_Text>().text = queryNotFoundMessage;
            return;
        }

        CreateFriendEntry(result.Value.userId, result.Value.displayName);
    }
    
    private void OnUsersFriendCodeFound(Result<PublicUserData> result, string query, Action fallbackAction = null)
    {
        friendSearchBar.enabled = true;
        
        if (result.IsError)
        {
            if (fallbackAction is not null)
            {
                fallbackAction.Invoke();
                return;
            }
            
            CurrentView = FindFriendsView.LoadFailed;
            string queryNotFoundMessage = FriendsHelper.QueryNotFoundMessage.Replace("%QUERY%", query);
            loadingFailedPanel.GetComponentInChildren<TMP_Text>().text = queryNotFoundMessage;
            return;
        }
        
        PublicUserData userData = result.Value;
        if (userData.userId == friendsEssentialsWrapper.PlayerUserId)
        {
            BytewarsLogger.Log("Found friend code with self entry");
            
            CurrentView = FindFriendsView.LoadFailed;
            loadingFailedPanel.GetComponentInChildren<TMP_Text>().text = FriendsHelper.FriendRequestSelfMessage;
            return;
        }

        SendFriendInvitation(userData.userId, usingFriendCode: true);
        CreateFriendEntry(userData.userId, userData.displayName);
    }
    
    private void OnSendRequestComplete(IResult result, bool usingFriendCode = false)
    {
        if (result.IsError)
        {
            if (usingFriendCode)
            {
                BytewarsLogger.LogWarning($"Unable to send friend request using friend code: {result.Error.Message}");
                return;
            }

            string errorMessage = FriendsHelper.DefaultSendFriendRequestErrorMessage;
            if (FriendsHelper.SendFriendRequestErrorMessages.TryGetValue(result.Error.Code, out string message))
            {
                errorMessage = message;
            }

            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle, errorMessage, "OK", null);
            return;
        }

        string promptDetailsMessage = usingFriendCode 
            ? FriendsHelper.FriendRequestSentFriendCodeMessage 
            : FriendsHelper.FriendRequestSentDetailsMessage;

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptMessageTitle, 
            promptDetailsMessage, "OK", null);
        
        if (!userResult.TryGetComponent(out FindFriendsEntryHandler entryHandler))
        {
            return;
        }
        
        entryHandler.SendInviteButton.interactable = false;
        entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = FriendsHelper.RequestSentMessage;
        entryHandler.FriendStatus.text = FriendsHelper.StatusMessageMap[RelationshipStatusCode.Outgoing];
    }
    
    private void OnGetFriendshipStatusCompleted(Result<FriendshipStatus> result)
    {
        if (userResult is null)
        {
            return;
        }
        
        FindFriendsEntryHandler entryHandler = userResult.GetComponent<FindFriendsEntryHandler>();
        if (result.IsError)
        {
            entryHandler.FriendStatus.text = FriendsHelper.ErrorStatusMessage;
            BytewarsLogger.LogWarning($"Unable to get friendship status: {result.Error.Message}");
            
            CurrentView = FindFriendsView.LoadFailed;
            return;
        }

        CurrentView = FindFriendsView.LoadSuccess;
        
        RelationshipStatusCode friendshipStatus = result.Value.friendshipStatus;
        
        string statusMessage = FriendsHelper.StatusMessageMap[friendshipStatus];
        entryHandler.FriendStatus.text = statusMessage;
        entryHandler.SendInviteButton.interactable = friendshipStatus is RelationshipStatusCode.NotFriend;

        if (friendshipStatus is RelationshipStatusCode.Outgoing)
        {
            entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = FriendsHelper.RequestSentMessage;
        }
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
        
        if (result.Value == null || userResult == null)
        {
            return;
        }
        
        Image entryImage = userResult.GetComponent<FindFriendsEntryHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        entryImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }
    
    #endregion Callback Functions
    
    #region View Management
    
    private void ClearSearchPanel()
    {
        resultContentPanel.DestroyAllChildren();

        if (userResult != null)
        {
            Destroy(userResult);
        }
    }
    
    private async void OnFriendCodeCopyButtonClicked()
    {
        GUIUtility.systemCopyBuffer = friendCode.text;
        TMP_Text buttonText = friendCodeCopyButton.GetComponentInChildren<TMP_Text>();
        
        string originalText = buttonText.text;
        buttonText.SetText(FriendsHelper.FriendCodeCopiedMessage);
        friendCodeCopyButton.interactable = false;
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        buttonText.SetText(originalText);
        friendCodeCopyButton.interactable = true;
    }
    
    private void CreateFriendEntry(string userId, string displayName)
    {
        if (userId == friendsEssentialsWrapper.PlayerUserId)
        {
            BytewarsLogger.Log("Skipping self entry");

            CurrentView = FindFriendsView.Default;
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
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;
        playerEntryHandler.SendInviteButton.onClick.AddListener(() => SendFriendInvitation(userId));
        
        userResult = playerEntry;
        
        CheckFriendshipStatus(userId);
        RetrieveUserAvatar(userId);
    }
    
    private void CheckFriendshipStatus(string userId)
    {
        friendsEssentialsWrapper.GetFriendshipStatus(userId, OnGetFriendshipStatusCompleted);
    }
    
    private void SetFriendCodeText(string friendCodeString)
    {
        if (string.IsNullOrEmpty(friendCodeString))
        {
            friendCode.SetText(FriendsHelper.FriendCodePreloadMessage);
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
