using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class FriendDetailsMenuHandler : MenuCanvas
{
    public RectTransform friendDetailsPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button promoteToLeaderButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button inviteToPartyButton;
    [SerializeField] private Button blockButton;
    [SerializeField] private Button unfriendButton;

    private string _userId;
    public string UserID { private get => _userId;
        set
        {
            _userId = value;
            Debug.Log(UserID);
        }}
    private ManagingFriendsWrapper _managingFriendsWrapper;
        
    // Start is called before the first frame update
    void Start()
    {
        EnableButton(blockButton, TutorialType.ManagingFriends);
        EnableButton(unfriendButton, TutorialType.ManagingFriends);
        
        _managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(OnBlockCliked);
        unfriendButton.onClick.AddListener(OnUnfriendClicked);
        
        // Party-related buttons setup
        promoteToLeaderButton.gameObject.SetActive(false);
        kickButton.gameObject.SetActive(false);
        inviteToPartyButton.gameObject.SetActive(true);
        
        promoteToLeaderButton.onClick.AddListener(OnPromoteToLeaderButtonClicked);
        kickButton.onClick.AddListener(OnKickButtonClicked);
        inviteToPartyButton.onClick.AddListener(OnInviteToPartyButtonClicked);
    }

    private void OnEnable()
    {
        UpdatePartyButtons();
    }

    private void EnableButton(Button button, TutorialType tutorialType)
    {
        var module = TutorialModuleManager.Instance.GetModule(tutorialType);
        if (module.isActive)
        {
            button.gameObject.SetActive(true);
        }
    }
    
    private void OnUnfriendClicked()
    {
        _managingFriendsWrapper.Unfriend(UserID, OnUnfriendCompleted);
    }

    private void OnUnfriendCompleted(Result result)
    {
        if (!result.IsError)
        {
            Debug.Log($"Successfully unfriend a friend with an ID {UserID}");
        }
        else
        {
            Debug.LogWarning($"Error ");
        }
    }

    private void OnBlockCliked()
    {
        _managingFriendsWrapper.BlockPlayer(UserID, OnBlockPlayerComplete);
    }

    private void OnBlockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        if (!result.IsError)
        {
            Debug.Log($"Successfully block a user with an ID {UserID}");
        }
        else
        {
            Debug.LogWarning($"Error ");
        }
    }

    #region Party Functions

    private void UpdatePartyButtons()
    {
        AuthEssentialsWrapper authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        if (authWrapper)
        {
            bool isCurrentlyLeader = authWrapper.userData.user_id == PartyHelper.CurrentLeaderUserId; // current user is leader
            bool isTheLeader = _userId == PartyHelper.CurrentLeaderUserId; // displayed friend details is the leader's info
            bool isInParty = PartyHelper.PartyMembersData.Any(data => data.UserId == _userId);
            promoteToLeaderButton.gameObject.SetActive(isCurrentlyLeader && isInParty);
            kickButton.gameObject.SetActive(isCurrentlyLeader && isInParty);
            inviteToPartyButton.gameObject.SetActive(!isTheLeader && !isInParty);
        }
    }

    private void OnPromoteToLeaderButtonClicked()
    {
        PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        partyHelper.PromoteToPartyLeader(UserID);
        UpdatePartyButtons();
    }
    
    private void OnKickButtonClicked()
    {
        PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        partyHelper.KickFromParty(UserID);
        UpdatePartyButtons();
    }
    
    private void OnInviteToPartyButtonClicked()
    {
        PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        partyHelper.InviteToParty(UserID);
        UpdatePartyButtons();
    }

    #endregion
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenuCanvas;
    }
}
