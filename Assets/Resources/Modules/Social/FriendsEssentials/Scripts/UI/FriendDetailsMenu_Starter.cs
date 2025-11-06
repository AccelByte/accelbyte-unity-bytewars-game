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

public class FriendDetailsMenu_Starter : MenuCanvas
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

    // TODO: Declare Module Wrappers here.

    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Update Party Buttons here.
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

        // TODO: Define Module Wrapper listeners here.
    }
    
    #region Add Friends Module
    
    private void AddFriend()
    {
        // TODO: Implement AddFriend function here.
        BytewarsLogger.LogWarning("AddFriend is not yet implemented.");
    }

    private void CancelFriendRequest()
    {
        // TODO: Implement CancelFriendRequest function here.
        BytewarsLogger.LogWarning("CancelFriendRequest is not yet implemented.");
    }
    
    private void AcceptFriendRequest()
    {
        // TODO: Implement AcceptFriendRequest function here.
        BytewarsLogger.LogWarning("AcceptFriendRequest is not yet implemented.");
    }
    
    private void RejectFriendRequest()
    {
        // TODO: Implement RejectFriendRequest function here.
        BytewarsLogger.LogWarning("RejectFriendRequest is not yet implemented.");
    }
    
    // TODO: Implement Friend Details functions here.
    
    #endregion Add Friends Module

    #region Manage Friends Module

    private void Unfriend()
    {
        // TODO: Implement Unfriend function here.
        BytewarsLogger.LogWarning("Unfriend is not yet implemented.");
    }
    
    private void BlockPlayer()
    {
        // TODO: Implement Block Player function here.
        BytewarsLogger.LogWarning("BlockPlayer is not yet implemented.");
    }
    
    private void UnblockPlayer()
    {
        // TODO: Implement Unblock Player function here.
        BytewarsLogger.LogWarning("Unblock is not yet implemented.");
    }

    private void OnUnfriendCompleted(Result result)
    {
        // TODO: Implement OnUnfriendCompleted function here.
        BytewarsLogger.LogWarning("OnUnfriendCompleted is not yet implemented.");
    }

    private void OnBlockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        // TODO: Implement OnBlockPlayerComplete function here.
        BytewarsLogger.LogWarning("OnBlockPlayerComplete is not yet implemented.");
    }
    
    private void OnUnblockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        // TODO: Implement OnUnblockPlayerComplete function here.
        BytewarsLogger.LogWarning("OnUnblockPlayerComplete is not yet implemented.");
    }

    // TODO: Implement Friend Details functions here.

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
    }
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenu_Starter;
    }
}
