// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FindFriendsMenu : MenuCanvas
{
    [Header("Find Friends Components")]
    [SerializeField] private GameObject friendEntryPrefab;
    [SerializeField] private TMP_Text friendCode;
    [SerializeField] private Button friendCodeCopyButton;
    [SerializeField] private TMP_InputField friendSearchBar;
    [SerializeField] private Button friendSearchButton;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform resultContentPanel;
    [SerializeField] private Button backButton;
    
    private GameObject userResult;
    
    private FriendsEssentialsWrapper friendsEssentialsWrapper;
    
    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        friendCodeCopyButton.onClick.AddListener(OnFriendCodeCopyButtonClicked);
        friendSearchBar.onSubmit.AddListener(FindFriend);
        friendSearchButton.onClick.AddListener(() => FindFriend(friendSearchBar.text));
    }
    
    private void Start()
    {
        friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();

        SetFriendCodeText();
    }
    
    private void OnDisable()
    {
        ClearSearchPanel();
        friendSearchBar.text = string.Empty;
    }

    private void OnEnable()
    {
        friendSearchBar.enabled = true;
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
    }

    #region Search for Players Module

    #region Main Functions

    private void FindFriend(string query)
    {
        if (string.IsNullOrEmpty(friendSearchBar.text) || string.IsNullOrEmpty(friendSearchBar.text))
        {
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
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
            MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsEssentialsModels.SendingFriendRequestMessage);
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
            string queryNotFoundMessage = FriendsEssentialsModels.QueryNotFoundMessage.Replace("%QUERY%", query);
            widgetSwitcher.ErrorMessage = queryNotFoundMessage;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        CreateFriendEntry(result.Value.userId, result.Value.displayName);
    }
    
    private void OnUsersFriendCodeFound(Result<AccountUserPlatformData> result, string query, Action fallbackAction = null)
    {
        friendSearchBar.enabled = true;
        
        if (result.IsError)
        {
            if (fallbackAction is not null)
            {
                fallbackAction.Invoke();
                return;
            }
            
            string queryNotFoundMessage = FriendsEssentialsModels.QueryNotFoundMessage.Replace("%QUERY%", query);
            widgetSwitcher.ErrorMessage = queryNotFoundMessage;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }
        
        AccountUserPlatformData userData = result.Value;
        if (userData.UserId == GameData.CachedPlayerState.PlayerId)
        {
            BytewarsLogger.Log("Found friend code with self entry");
            
            widgetSwitcher.ErrorMessage = FriendsEssentialsModels.FriendRequestSelfMessage;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        SendFriendInvitation(userData.UserId, usingFriendCode: true);
        CreateFriendEntry(userData.UserId, userData.DisplayName);
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

            string errorMessage = FriendsEssentialsModels.DefaultSendFriendRequestErrorMessage;
            if (FriendsEssentialsModels.SendFriendRequestErrorMessages.TryGetValue(result.Error.Code, out string message))
            {
                errorMessage = message;
            }

            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle, errorMessage, "OK", null);
            return;
        }

        string promptDetailsMessage = usingFriendCode 
            ? FriendsEssentialsModels.FriendRequestSentFriendCodeMessage 
            : FriendsEssentialsModels.FriendRequestSentDetailsMessage;

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptMessageTitle, 
            promptDetailsMessage, "OK", null);
        
        if (!userResult.TryGetComponent(out FriendEntry entryHandler))
        {
            return;
        }
        
        entryHandler.SendInviteButton.interactable = false;
        entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = FriendsEssentialsModels.RequestSentMessage;
        entryHandler.FriendStatus.text = FriendsEssentialsModels.StatusMessageMap[RelationshipStatusCode.Outgoing];
    }
    
    private void OnGetFriendshipStatusCompleted(Result<FriendshipStatus> result)
    {
        if (userResult is null)
        {
            return;
        }
        
        FriendEntry entryHandler = userResult.GetComponent<FriendEntry>();
        if (result.IsError)
        {
            entryHandler.FriendStatus.text = FriendsEssentialsModels.ErrorStatusMessage;
            BytewarsLogger.LogWarning($"Unable to get friendship status: {result.Error.Message}");

            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);

        RelationshipStatusCode friendshipStatus = result.Value.friendshipStatus;
        
        string statusMessage = FriendsEssentialsModels.StatusMessageMap[friendshipStatus];
        entryHandler.FriendStatus.text = statusMessage;
        entryHandler.SendInviteButton.interactable = friendshipStatus is RelationshipStatusCode.NotFriend;

        if (friendshipStatus is RelationshipStatusCode.Outgoing)
        {
            entryHandler.SendInviteButton.GetComponentInChildren<TMP_Text>().text = FriendsEssentialsModels.RequestSentMessage;
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
        
        Image entryImage = userResult.GetComponent<FriendEntry>().FriendImage;
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
        AccelByteWarsUtility.CopyToClipboard(friendCode.text);
        TMP_Text buttonText = friendCodeCopyButton.GetComponentInChildren<TMP_Text>();
        
        string originalText = buttonText.text;
        buttonText.SetText(FriendsEssentialsModels.FriendCodeCopiedMessage);
        friendCodeCopyButton.interactable = false;
        
        await UniTask.Delay(TimeSpan.FromSeconds(2));
        
        buttonText.SetText(originalText);
        friendCodeCopyButton.interactable = true;
    }
    
    private void CreateFriendEntry(string userId, string displayName)
    {
        if (userId == GameData.CachedPlayerState.PlayerId)
        {
            BytewarsLogger.Log("Skipping self entry");

            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
            return;
        }
        
        GameObject playerEntry = Instantiate(friendEntryPrefab, resultContentPanel);
        playerEntry.name = userId;
        
        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }
        
        FriendEntry playerEntryHandler = playerEntry.GetComponent<FriendEntry>();
        playerEntryHandler.EntryView = FriendEntry.FriendEntryView.Searched;
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
    
    private void SetFriendCodeText()
    {
        friendCode.SetText(FriendsEssentialsModels.FriendCodePreloadMessage);
        friendsEssentialsWrapper.GetSelfFriendCode((Result<string> result) =>
        {
            if (!result.IsError)
            {
                friendCode.SetText(result.Value);
            }
        });
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
        return AssetEnum.FindFriendsMenu;
    }
    
    #endregion MenuCanvas Overrides
}
