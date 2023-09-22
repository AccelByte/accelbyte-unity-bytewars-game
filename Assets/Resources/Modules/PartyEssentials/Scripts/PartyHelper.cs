using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyHelper : MonoBehaviour
{
    [SerializeField] private GameObject partyInvitationPrefab;
    [SerializeField] private GameObject messageNotificationPrefab;
    
    private PartyEssentialsWrapper _partyWrapper;
    private PartyHandler _partyHandler;
    
    private const string PARTY_SESSION_TEMPLATE_NAME = "unity-party";
    
    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();
        partyInvitationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PartyInvitationEntryPanel) as GameObject;
        messageNotificationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.MessageNotificationEntryPanel) as GameObject;
        
        LoginHandler.onLoginCompleted += data =>
        {
            CheckPartyStatus();
            SubscribeLobbyNotifications();
        };
    }

    private void TriggerMessageNotification(string messageText)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        MessageNotificationEntryPanel messageNotificationPanel = notificationHandler.AddNotificationItem<MessageNotificationEntryPanel>(messageNotificationPrefab);
        messageNotificationPanel.ChangeMessageText(messageText);
    }


    #region Lobby Notification Callback Functions

    private void SubscribeLobbyNotifications()
    {
        Lobby lobby = MultiRegistry.GetApiClient().GetLobby();
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }

        // current user related notification
        lobby.SessionV2InvitedUserToParty += OnReceivePartyInvitation;
        lobby.SessionV2UserKickedFromParty += OnKickedFromParty;
            
        // other users related notification
        lobby.SessionV2PartyUpdated += OnPartyUpdated;
        lobby.SessionV2PartyMemberChanged += OnPartyMemberChanged;
    }

    private void OnReceivePartyInvitation(Result<SessionV2PartyInvitationNotification> invitation)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        PartyInvitationEntryPanel invitationEntryPanel = notificationHandler.AddNotificationItem<PartyInvitationEntryPanel>(partyInvitationPrefab);
        invitationEntryPanel.UpdatePartyInvitationInfo(invitation.Value.partyId, invitation.Value.senderId);
    }

    private void OnKickedFromParty(Result<SessionV2PartyUserKickedNotification> result)
    {
        _partyHandler.ResetPartyMemberEntryUI();

        _partyWrapper.partyId = "";
        _partyHandler.currentPartyId = "";
        _partyHandler.currentLeaderUserId = "";
    }
    
    private void OnPartyUpdated(Result<SessionV2PartySessionUpdatedNotification> result)
    {
        Debug.Log($"[PartyNotif] SessionV2PartyUpdated - {result.Value.members.Length}");
        _partyHandler.UpdatePartyMembersData(result.Value.members, result.Value.leaderId);
    }
    
    private void OnPartyMemberChanged(Result<SessionV2PartyMembersChangedNotification> result)
    {
        Debug.Log($"[PartyNotif] SessionV2PartyMemberChanged - {result.Value.session.members.Length}");
        foreach (SessionV2MemberData member in result.Value.session.members)
        {
            Debug.Log($"[PartyNotif] Looping.. => {member.id} - {member.status}");
            if (member.id == result.Value.joinerId)
            {
                if (member.status == SessionV2MemberStatus.JOINED || member.status == SessionV2MemberStatus.KICKED)
                {
                    _partyHandler.UpdatePartyMembersData(result.Value.session.members, result.Value.leaderId);
                }
            }
            break;
        }
    }

    #endregion

    #region Party Service Helper Functions

    private void CheckPartyStatus()
    {
        _partyWrapper.GetUserParties(OnGetUserPartiesCompleted);
    }
    
    public void JoinedParty(SessionV2PartySession partySession)
    {
        _partyHandler.currentPartyId = partySession.id;
        _partyHandler.currentLeaderUserId = partySession.leaderId;
        _partyHandler.UpdatePartyMembersData(partySession.members, partySession.leaderId);
    }
    
    public void PromoteToPartyLeader(string userId)
    {
        _partyWrapper.PromoteMemberToPartyLeader(_partyWrapper.partyId, userId, result =>
        {
            if (!result.IsError)
            {
                TriggerMessageNotification($"{userId} promoted to leader!");
            }
        });
    }

    public void KickFromParty(string userId)
    {
        _partyWrapper.KickMemberFromParty(_partyWrapper.partyId, userId, result =>
        {
            if (!result.IsError)
            {
                TriggerMessageNotification($"{userId} kicked from party!");
            }
        });
    }

    public void InviteToParty(string inviteeUserId)
    {
        if (string.IsNullOrEmpty(_partyWrapper.partyId))
        {
            _partyWrapper.CreateParty(PARTY_SESSION_TEMPLATE_NAME, result => OnCreatePartyCompleted(result, inviteeUserId));
        }
        else
        {
            _partyWrapper.SendPartyInvitation(_partyWrapper.partyId, inviteeUserId, OnSendPartyInvitationCompleted);
        }
    }
    
    #endregion

    #region Party Service Callback Functions

    private void OnGetUserPartiesCompleted(Result<PaginatedResponse<SessionV2PartySession>> result)
    {
        if (!result.IsError)
        {
            foreach (SessionV2PartySession partySession in result.Value.data)
            {
                _partyWrapper.LeaveParty(partySession.id, null);
            }
        }
    }
    
    private void OnCreatePartyCompleted(Result<SessionV2PartySession> result, string inviteeUserId)
    {
        Debug.Log("[PARTY] Successfully create a new party!");
        TriggerMessageNotification("You've just created a new party!");

        // update party data
        PartyHandler partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();
        partyHandler.SetLeaveButtonInteractable(true);
        partyHandler.currentPartyId = result.Value.id;
        partyHandler.currentLeaderUserId = result.Value.leaderId;
        partyHandler.UpdatePartyMembersData(result.Value.members, result.Value.leaderId);
        
        _partyWrapper.SendPartyInvitation(_partyWrapper.partyId, inviteeUserId, OnSendPartyInvitationCompleted);
    }
    
    private void OnSendPartyInvitationCompleted(Result result)
    {
        Debug.Log($"[PARTY] Sending party invitation..");
        TriggerMessageNotification("You've just sent out a party invitation! Waiting for a response..");
    }

    #endregion
}