using System;
using System.Collections;
using System.Collections.Generic;
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
        EnableButton(promoteToLeaderButton, TutorialType.PartyEssentials);
        EnableButton(kickButton, TutorialType.PartyEssentials);
        EnableButton(inviteToPartyButton, TutorialType.PartyEssentials);
        
        _managingFriendsWrapper = TutorialModuleManager.Instance.GetModuleClass<ManagingFriendsWrapper>();
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(OnBlockCliked);
        unfriendButton.onClick.AddListener(OnUnfriendClicked);
        
        PartyHandler partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();
        promoteToLeaderButton.interactable = _userId != partyHandler.currentLeaderUserId;
        kickButton.interactable = _userId != partyHandler.currentLeaderUserId;
        
        PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        promoteToLeaderButton.onClick.AddListener(() =>
        {
            partyHelper.PromoteToPartyLeader(UserID);
        });
        kickButton.onClick.AddListener(() =>
        {
            partyHelper.KickFromParty(UserID);
        });
        inviteToPartyButton.onClick.AddListener(() =>
        {
            partyHelper.InviteToParty(UserID);
        });
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

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenuCanvas;
    }
}
