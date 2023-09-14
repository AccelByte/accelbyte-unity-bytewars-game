using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler : MenuCanvas
{
    [SerializeField] private Transform[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject partyInvitationPrefab;

    private PartyEssentialsWrapper _partyWrapper;
    private Lobby _lobby;
    
    // Start is called before the first frame update
    void Start()
    {
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        
        _lobby = MultiRegistry.GetApiClient().GetLobby();
        _lobby.SessionV2InvitedUserToParty += ReceivePartyInvitation;
        _lobby.SessionV2UserJoinedParty += OnUserJoinedParty;
    }

    private void OnLeaveButtonClicked()
    {
        _partyWrapper.LeaveParty(_partyWrapper.partyId, null);
    }

    private void ReceivePartyInvitation(Result<SessionV2PartyInvitationNotification> invitation)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        PartyInvitationEntryPanel invitationEntryPanel = notificationHandler.AddNotificationItem<PartyInvitationEntryPanel>(partyInvitationPrefab);
        invitationEntryPanel.UpdatePartyInvitationInfo(invitation.Value.partyId, invitation.Value.senderId);
    }
    
    private void OnUserJoinedParty(Result<SessionV2PartyJoinedNotification> result)
    {
        int memberIndex = result.Value.members.Length - 1;
        PartyMemberEntryPanel partyMemberEntryPanel = partyMemberEntryPanels[memberIndex].GetComponent<PartyMemberEntryPanel>();
        partyMemberEntryPanel.SwitchView(PartyMemberEntryPanel.PartyEntryView.MemberInfo);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    public override GameObject GetFirstButton()
    {
        return leaveButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenuCanvas;
    }
}
