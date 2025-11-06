// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendDetailsMenu : MenuCanvas
{
    [Header("Friend Details"), SerializeField] private Image friendImage;
    [SerializeField] private TMP_Text friendDisplayName;
    [SerializeField] private TMP_Text friendPresence;

    [Header("Friend Components"), SerializeField] private Button addFriendButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button unfriendButton;
    [SerializeField] private Button blockButton;
    [SerializeField] private Button unblockButton;

    [Header("Party Components"), SerializeField] private Button promoteToLeaderButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button inviteToPartyButton;
    
    [Header("Menu Components"), SerializeField] private Button backButton;
    
    public Image FriendImage => friendImage;
    public TMP_Text FriendDisplayName => friendDisplayName;
    public TMP_Text FriendPresence => friendPresence;

    public string UserId { get; set; } = string.Empty;

    private ManagingFriendsWrapper managingFriendsWrapper;
    private FriendsEssentialsWrapper friendsEssentialsWrapper;
    
    private void OnEnable()
    {
        if (managingFriendsWrapper == null)
        {
            managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        }

        if (managingFriendsWrapper != null && !string.IsNullOrEmpty(UserId))
        {
            managingFriendsWrapper.GetBlockedPlayers(OnGetBlockedPlayers);
        }

        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }

        if (friendsEssentialsWrapper != null && !string.IsNullOrEmpty(UserId))
        {
            friendsEssentialsWrapper.GetFriendshipStatus(UserId, OnGetFriendshipStatusCompleted);
        }

        DisableAllButtons();
        UpdatePartyButtons();
    }
    
    private void Awake()
    {
        EnableButtonByModule(blockButton, TutorialType.ManagingFriends);
        EnableButtonByModule(unfriendButton, TutorialType.ManagingFriends);
        
        backButton.onClick.AddListener(OnBackPressed);
        
        addFriendButton.onClick.AddListener(AddFriend);
        cancelButton.onClick.AddListener(CancelFriendRequest);
        acceptButton.onClick.AddListener(AcceptFriendRequest);
        rejectButton.onClick.AddListener(RejectFriendRequest);
        unfriendButton.onClick.AddListener(Unfriend);
        blockButton.onClick.AddListener(BlockPlayer);
        unblockButton.onClick.AddListener(UnblockPlayer);

        InitializePartyButtons();

        // Bind to both to support mixed usage of starter and non modules
        ManagingFriendsModels.OnPlayerUnfriended += OnUnfriended;
        ManagingFriendsModels.OnPlayerBlocked += OnBlocked;
        FriendsEssentialsModels.OnIncomingRequest += OnIncomingRequest;
        FriendsEssentialsModels.OnRequestAccepted += OnRequestAccepted;
        FriendsEssentialsModels.OnRequestCanceled += OnRequestCanceled;
        FriendsEssentialsModels.OnRequestRejected += OnRequestRejected;
    }
    
    #region Add Friends Module

    #region Main Functions

    private void AddFriend()
    {
        friendsEssentialsWrapper.SendFriendRequest(UserId, OnAddfriendCompleted);
        addFriendButton.gameObject.SetActive(false);
    }

    private void CancelFriendRequest()
    {
        friendsEssentialsWrapper.CancelFriendRequests(UserId, OnFriendRequestCanceled);
        cancelButton.gameObject.SetActive(false);
    }
    
    private void AcceptFriendRequest()
    {
        friendsEssentialsWrapper.AcceptFriend(UserId, OnAcceptFriendCompleted);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
    }
    
    private void RejectFriendRequest()
    {
        friendsEssentialsWrapper.DeclineFriend(UserId, OnRejectFriendRequest);
        rejectButton.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
    }
    
    #endregion Main Functions
    
    #region Callback Functions
    private void OnGetFriendshipStatusCompleted(Result<FriendshipStatus> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }
        
        switch (result.Value.friendshipStatus)
        {
            case RelationshipStatusCode.Friend:
                EnableButtonByModule(unfriendButton, TutorialType.ManagingFriends);
                break;
            case RelationshipStatusCode.Incoming:
                EnableButtonByModule(rejectButton, TutorialType.ManagingFriends);
                EnableButtonByModule(acceptButton, TutorialType.ManagingFriends);
                break;
            case RelationshipStatusCode.Outgoing:
                EnableButtonByModule(cancelButton, TutorialType.ManagingFriends);
                break;
            case RelationshipStatusCode.NotFriend:
                EnableButtonByModule(addFriendButton, TutorialType.ManagingFriends);
                break;
        }

        BytewarsLogger.Log($"OnGetFriendshipStatusCompleted: {UserId}");
    }
    
    private void OnAddfriendCompleted(Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully add player with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.DefaultSendFriendRequestErrorMessage : FriendsEssentialsModels.FriendRequestSentDetailsMessage
        });
        cancelButton.gameObject.SetActive(true);
    }
    
    private void OnFriendRequestCanceled (Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully canceled friend request with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.FriendRequestCanceledMessage
        });
        
        addFriendButton.gameObject.SetActive(true);
    }
    
    private void OnAcceptFriendCompleted (Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully accepted friend request with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.FriendRequestAcceptedMessage
        });
        
        unfriendButton.gameObject.SetActive(true);
    }
    
    private void OnRejectFriendRequest (Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully accepted friend request with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.FriendRequestRejectedMessage
        });
        
        addFriendButton.gameObject.SetActive(true);
    }
    
    private void OnRequestRejected (string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }
        
        addFriendButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(false);
    }
    private void OnRequestCanceled (string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }
        addFriendButton.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
    }
    private void OnIncomingRequest (string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }
        acceptButton.gameObject.SetActive(true);
        rejectButton.gameObject.SetActive(true);
        addFriendButton.gameObject.SetActive(false);
    }
    private void OnRequestAccepted (string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }
        unfriendButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(false);
    }
    
    #endregion Callback Functions
    
    #endregion Add Friends Module

    #region Manage Friends Module

    #region Main Functions

    private void Unfriend()
    {
        managingFriendsWrapper.Unfriend(UserId, OnUnfriendCompleted);
        unfriendButton.gameObject.SetActive(false);
    }
    
    private void BlockPlayer()
    {
        managingFriendsWrapper.BlockPlayer(UserId, OnBlockPlayerCompleted);
        blockButton.gameObject.SetActive(false);
    }

    private void UnblockPlayer()
    {
        managingFriendsWrapper.UnblockPlayer(UserId, OnUnblockPlayerCompleted);
        unblockButton.gameObject.SetActive(false);
    }
    
    #endregion Main Functions

    #region Callback Functions
    
    private void OnGetBlockedPlayers (Result<BlockedList> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        foreach (BlockedData blockedData in result.Value.data)
        {
            if (UserId.Equals(blockedData.blockedUserId))
            {
                EnableButtonByModule(unblockButton, TutorialType.ManagingFriends);
                return;
            }
        }
        EnableButtonByModule(blockButton, TutorialType.ManagingFriends);
    }

    private void OnUnfriendCompleted(Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully unfriended player with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.UnfriendCompletedMessage
        });
        
        addFriendButton.gameObject.SetActive(true);
    }

    private void OnBlockPlayerCompleted(Result<BlockPlayerResponse> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }
        
        BytewarsLogger.Log($"Successfully blocked player with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.BlockPlayerCompletedMessage
        });

        DisableAllButtons();
        unblockButton.gameObject.SetActive(true);
    }
    
    private void OnUnblockPlayerCompleted (Result<UnblockPlayerResponse> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully unblocked player with user ID: {UserId}");

        MenuManager.Instance.PushNotification(new PushNotificationModel 
        {
            Message = result.IsError ? FriendsEssentialsModels.ErrorStatusMessage : FriendsEssentialsModels.UnblockPlayerCompletedMessage
        });

        addFriendButton.gameObject.SetActive(true);
        blockButton.gameObject.SetActive(true);
        UpdatePartyButtons();
    }
    
    private void OnUnfriended (string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }
        
        addFriendButton.gameObject.SetActive(true);
        unfriendButton.gameObject.SetActive(false);
    }
    
    private void OnBlocked(string userId)
    {
        if (!gameObject.activeSelf || userId != UserId)
        {
            return;
        }

        DisableAllButtons();
    }

    #endregion Callback Functions

    #endregion Manage Friends Module

    #region Party Module

    private void InitializePartyButtons()
    {
        inviteToPartyButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnInviteToPartyButtonClicked(UserId); });
        promoteToLeaderButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnPromotePartyLeaderButtonClicked(UserId); });
        kickButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnKickPlayerFromPartyButtonClicked(UserId); });

        AccelByteWarsOnlineSession.OnPartySessionUpdated += UpdatePartyButtons;

        // Update party button states after initialization.
        UpdatePartyButtons();
    }

    private void UpdatePartyButtons()
    {
        ModuleModel partyModule = TutorialModuleManager.Instance.GetModule(TutorialType.PartyEssentials);
        bool isPartyModuleActive = partyModule != null && partyModule.isActive;

        SessionV2PartySession partySession = AccelByteWarsOnlineSession.CachedParty;
        PlayerState currentUser = GameData.CachedPlayerState;

        bool isFriendInParty = false, isCurrentUserIsLeader = false;
        if (currentUser != null)
        {
            isFriendInParty =
                partySession == null ? false :
                partySession.members.
                Where(x => x.StatusV2 == SessionV2MemberStatus.JOINED).
                Select(x => x.id).Contains(UserId);

            isCurrentUserIsLeader =
                partySession != null &&
                partySession.leaderId == (currentUser == null ? string.Empty : currentUser.PlayerId);
        }

        inviteToPartyButton.gameObject.SetActive(isPartyModuleActive && !isFriendInParty);
        promoteToLeaderButton.gameObject.SetActive(isPartyModuleActive && isFriendInParty && isCurrentUserIsLeader);
        kickButton.gameObject.SetActive(isPartyModuleActive && isFriendInParty && isCurrentUserIsLeader);
    }

    #endregion Party Module

    private static void EnableButtonByModule(Button button, TutorialType tutorialType)
    {
        bool moduleActive = TutorialModuleManager.Instance.IsModuleActive(tutorialType);

        button.gameObject.SetActive(moduleActive);
    }
    
    private void OnBackPressed()
    {
        if (GameManager.Instance.InGamePause != null && GameManager.Instance.InGamePause.IsPausing())
        {
            GameManager.Instance.InGamePause.BackToPreviousPauseMenu();
        }
        else
        {
            MenuManager.Instance.OnBackPressed();
        }
    }
    
    void DisableAllButtons()
    {
        addFriendButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
        unfriendButton.gameObject.SetActive(false);
        blockButton.gameObject.SetActive(false);
        unblockButton.gameObject.SetActive(false);
        inviteToPartyButton.gameObject.SetActive(false);
        promoteToLeaderButton.gameObject.SetActive(false);
        kickButton.gameObject.SetActive(false);
    }
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenu;
    }
}
