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

    [Header("Friend Components"), SerializeField] private Button blockButton;
    [SerializeField] private Button unfriendButton;

    [Header("Party Components"), SerializeField] private Button promoteToLeaderButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button inviteToPartyButton;
    
    [Header("Menu Components"), SerializeField] private Button backButton;
    
    public Image FriendImage => friendImage;
    public TMP_Text FriendDisplayName => friendDisplayName;
    public TMP_Text FriendPresence => friendPresence;

    public string UserId { get; set; } = string.Empty;

    private ManagingFriendsWrapper managingFriendsWrapper;
    
    private void OnEnable()
    {
        if (managingFriendsWrapper == null)
        {
            managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        }

        UpdatePartyButtons();
    }
    
    private void Awake()
    {
        EnableButtonByModule(blockButton, TutorialType.ManagingFriends);
        EnableButtonByModule(unfriendButton, TutorialType.ManagingFriends);
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(BlockPlayer);
        unfriendButton.onClick.AddListener(Unfriend);

        InitializePartyButtons();

        // Bind to both to support mixed usage of starter and non modules
        ManagingFriendsModels.OnPlayerUnfriended += OnUnfriendedOrBlocked;
        ManagingFriendsModels.OnPlayerUnfriended += OnUnfriendedOrBlocked;
    }

    #region Manage Friends Module

    #region Main Functions

    private void Unfriend()
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            FriendsEssentialsModels.PromptConfirmTitle,
            FriendsEssentialsModels.UnfriendConfirmationMessage, 
            "Yes", 
            confirmAction: () =>
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsEssentialsModels.UnfriendingMessage);

                managingFriendsWrapper.Unfriend(UserId, OnUnfriendCompleted);
            }, 
            "No", null);
    }

    private void BlockPlayer()
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            FriendsEssentialsModels.PromptConfirmTitle,
            FriendsEssentialsModels.BlockPlayerConfirmationMessage, 
            "Yes", 
            confirmAction: () =>
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsEssentialsModels.BlockingPlayerMessage);

                managingFriendsWrapper.BlockPlayer(UserId, OnBlockPlayerComplete);
            }, 
            "No", null);
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnUnfriendCompleted(Result result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully unfriended player with user ID: {UserId}");

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptMessageTitle,
            FriendsEssentialsModels.UnfriendCompletedMessage, "OK", null);

        MenuManager.Instance.OnBackPressed();
    }

    private void OnBlockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }
        
        BytewarsLogger.Log($"Successfully blocked player with user ID: {UserId}");

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsEssentialsModels.PromptMessageTitle,
            FriendsEssentialsModels.BlockPlayerCompletedMessage, "OK", null);

        MenuManager.Instance.OnBackPressed();
    }
    
    private void OnUnfriendedOrBlocked(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (userId != UserId)
        {
            return;
        }

        MenuManager.Instance.OnBackPressed();
    }

    #endregion Callback Functions

    #endregion Manage Friends Module

    #region Party Module

    private void InitializePartyButtons()
    {
        inviteToPartyButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnInviteToPartyButtonClicked(UserId); });
        promoteToLeaderButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnPromotePartyLeaderButtonClicked(UserId); });
        kickButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnKickPlayerFromPartyButtonClicked(UserId); });

        PartyEssentialsModels.PartyHelper.BindOnPartyUpdate(UpdatePartyButtons);

        // Update party button states after initialization.
        UpdatePartyButtons();
    }

    private void UpdatePartyButtons()
    {
        ModuleModel partyModule = TutorialModuleManager.Instance.GetModule(TutorialType.PartyEssentials);
        bool isPartyModuleActive = partyModule != null && partyModule.isActive;

        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
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
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenu;
    }
}
