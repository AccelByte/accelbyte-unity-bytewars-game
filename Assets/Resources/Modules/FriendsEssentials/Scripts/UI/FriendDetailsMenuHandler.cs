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

public class FriendDetailsMenuHandler : MenuCanvas
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
    private AuthEssentialsWrapper authEssentialsWrapper;
    private PartyHelper partyHelper;

    private void OnEnable()
    {
        if (managingFriendsWrapper == null)
        {
            managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        }
        
        if (authEssentialsWrapper == null)
        {
            authEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        }
        
        if (partyHelper == null)
        {
            partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        }
        
        if (managingFriendsWrapper != null && authEssentialsWrapper != null && partyHelper != null)
        {
            UpdatePartyButtons();
        }
    }
    
    private void Awake()
    {
        EnableButtonByModule(blockButton, TutorialType.ManagingFriends);
        EnableButtonByModule(unfriendButton, TutorialType.ManagingFriends);
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(BlockPlayer);
        unfriendButton.onClick.AddListener(Unfriend);
        
        promoteToLeaderButton.onClick.AddListener(PromoteToPartyLeader);
        kickButton.onClick.AddListener(KickFromParty);
        inviteToPartyButton.onClick.AddListener(InviteToParty);
        
        InitializePartyButtons(false);

        FriendsEssentialsWrapper.OnUnfriended += OnUnfriendedOrBlocked;
        ManagingFriendsWrapper.OnPlayerBlocked += OnUnfriendedOrBlocked;
    }

    #region Friend List Module

    #region Main Functions

    private void Unfriend()
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            FriendsHelper.PromptConfirmTitle,
            FriendsHelper.UnfriendConfirmationMessage, 
            "Yes", 
            confirmAction: () =>
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.UnfriendingMessage);

                managingFriendsWrapper.Unfriend(UserId, OnUnfriendCompleted);
            }, 
            "No", null);
    }

    private void BlockPlayer()
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            FriendsHelper.PromptConfirmTitle,
            FriendsHelper.BlockPlayerConfirmationMessage, 
            "Yes", 
            confirmAction: () =>
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(FriendsHelper.BlockingPlayerMessage);

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
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }

        BytewarsLogger.Log($"Successfully unfriend a user with an ID {UserId}");

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptMessageTitle,
            FriendsHelper.UnfriendCompletedMessage, "OK", null);

        MenuManager.Instance.OnBackPressed();
    }

    private void OnBlockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        if (result.IsError)
        {
            MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptErrorTitle,
                result.Error.Message, "OK", null);
            return;
        }
        
        BytewarsLogger.Log($"Successfully blocked user with user Id: {UserId}");

        MenuManager.Instance.PromptMenu.ShowPromptMenu(FriendsHelper.PromptMessageTitle,
            FriendsHelper.BlockPlayerCompletedMessage, "OK", null);

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

    #endregion Friend List Module

    #region Party Module

    #region Main Functions

    private void InviteToParty()
    {
        partyHelper.InviteToParty(UserId);

        UpdatePartyButtons();
    }

    private void PromoteToPartyLeader()
    {
        partyHelper.PromoteToPartyLeader(UserId);

        UpdatePartyButtons();
    }
    
    private void KickFromParty()
    {
        partyHelper.KickFromParty(UserId);

        UpdatePartyButtons();
    }

    #endregion Main Functions

    #region View Management

    private void InitializePartyButtons(bool inParty)
    {
        promoteToLeaderButton.gameObject.SetActive(!inParty);
        kickButton.gameObject.SetActive(!inParty);
        inviteToPartyButton.gameObject.SetActive(inParty);
    }
    
    private void UpdatePartyButtons()
    {
        if (!authEssentialsWrapper || authEssentialsWrapper.UserData == null)
        {
            return;
        }
        
        bool selfPartyLeader = authEssentialsWrapper.UserData.user_id == PartyHelper.CurrentLeaderUserId;
        bool partyLeader = UserId == PartyHelper.CurrentLeaderUserId;
        bool inParty = PartyHelper.PartyMembersData.Any(data => data.UserId == UserId);

        promoteToLeaderButton.gameObject.SetActive(selfPartyLeader && inParty);
        kickButton.gameObject.SetActive(selfPartyLeader && inParty);
        inviteToPartyButton.gameObject.SetActive(!partyLeader && !inParty);
    }
    
    #endregion View Management

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
        return AssetEnum.FriendDetailsMenuCanvas;
    }
}
