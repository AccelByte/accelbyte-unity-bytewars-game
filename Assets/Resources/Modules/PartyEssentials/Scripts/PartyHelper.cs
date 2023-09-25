using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyHelper : MonoBehaviour
{
    [SerializeField] private GameObject _partyInvitationPrefab;
    [SerializeField] private GameObject _messageNotificationPrefab;

    private PartyEssentialsWrapper _partyWrapper;
    private AuthEssentialsWrapper _authWrapper;
    private PartyHandler _partyHandler;

    private const string PARTY_SESSION_TEMPLATE_NAME = "unity-party";
    private const string DEFAULT_DISPLAY_NAME = "Player-";

    // Start is called before the first frame update
    void Start()
    {
        _partyInvitationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PartyInvitationEntryPanel) as GameObject;
        _messageNotificationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.MessageNotificationEntryPanel) as GameObject;
        
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        _partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();

        LoginHandler.onLoginCompleted += data =>
        {
            CheckPartyStatus();
            SubscribeLobbyNotifications();
        };
    }

    private void TriggerMessageNotification(string messageText)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        MessageNotificationEntryPanel messageNotificationPanel = notificationHandler.AddNotificationItem<MessageNotificationEntryPanel>(_messageNotificationPrefab);
        messageNotificationPanel.ChangeMessageText(messageText);
    }

    private void UpdatePartyMembersData(SessionV2MemberData[] members, string leaderId = null)
    {
        // set current party's leader id
        if (leaderId != "")
        {
            _partyHandler.currentLeaderUserId = leaderId;
        }
        
        // get members' user info data
        List<string> membersUserId = members.Select(member => member.id).ToList();
        _authWrapper.BulkGetUserInfo(membersUserId.ToArray(), result =>
        {
            if (!result.IsError)
            {
                List<PartyMemberData> partyMemberDatas = new List<PartyMemberData>();
                foreach (BaseUserInfo userData in result.Value.data)
                {
                    string displayName = userData.displayName == ""
                        ? DEFAULT_DISPLAY_NAME + userData.userId.Substring(0, 5)
                        : userData.displayName;
                    _authWrapper.GetUserAvatar(userData.userId, avatarResult =>
                    {
                        PartyMemberData memberData = new PartyMemberData(userData.userId, displayName, avatarResult.Value);
                        partyMemberDatas.Add(memberData);

                        if (partyMemberDatas.Count == membersUserId.Count)
                        {
                            _partyHandler.MembersUserInfo = partyMemberDatas.OrderBy(data => membersUserId.IndexOf(data.UserId)).ToList();
                            if (_partyHandler.gameObject.activeSelf)
                            {
                                _partyHandler.DisplayPartyMembersInfo();
                            }
                        }
                    });
                }
            }
        });
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
        PartyInvitationEntryPanel invitationEntryPanel =
            notificationHandler.AddNotificationItem<PartyInvitationEntryPanel>(_partyInvitationPrefab);
        invitationEntryPanel.UpdatePartyInvitationInfo(invitation.Value.partyId, invitation.Value.senderId);
    }

    private void OnKickedFromParty(Result<SessionV2PartyUserKickedNotification> result)
    {
        _partyHandler.ResetPartyMemberEntryUI();
        _partyHandler.DisplayOnlyCurrentPlayer();

        _partyWrapper.partyId = "";
        _partyHandler.currentPartyId = "";
        _partyHandler.currentLeaderUserId = "";
    }

    private void OnPartyUpdated(Result<SessionV2PartySessionUpdatedNotification> result)
    {
        UpdatePartyMembersData(result.Value.members, result.Value.leaderId);
    }

    private void OnPartyMemberChanged(Result<SessionV2PartyMembersChangedNotification> result)
    {
        List<SessionV2MemberData> members = result.Value.session.members.ToList();
        
        bool isUpdated = false;
        foreach (SessionV2MemberData member in result.Value.session.members)
        {
            if (member.id == result.Value.joinerId)
            {
                if (member.status == SessionV2MemberStatus.JOINED)
                {
                    isUpdated = true;
                }
            }

            SessionV2MemberStatus[] excludeConditions = {SessionV2MemberStatus.LEFT, SessionV2MemberStatus.KICKED};
            if (excludeConditions.Contains(member.status))
            {
                members.Remove(member);
                isUpdated = true;
            }
        }

        if (isUpdated)
        {
            UpdatePartyMembersData(members.ToArray(), result.Value.leaderId);
        }
    }

    #endregion

    #region Party Service Helper Functions

    private void CheckPartyStatus()
    {
        _partyWrapper.GetUserParties(OnGetUserPartiesCompleted);
    }

    public void OnJoinedParty(SessionV2PartySession partySession)
    {
        _partyHandler.currentPartyId = partySession.id;
        _partyHandler.currentLeaderUserId = partySession.leaderId;
        _partyHandler.SetLeaveButtonInteractable(true);
        UpdatePartyMembersData(partySession.members, partySession.leaderId);
    }

    public void PromoteToPartyLeader(string userId)
    {
        _partyWrapper.PromoteMemberToPartyLeader(_partyWrapper.partyId, userId, result =>
        {
            if (!result.IsError)
            {
                TriggerMessageNotification($"{DEFAULT_DISPLAY_NAME + userId.Substring(0,5)} promoted to leader!");
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
            _partyWrapper.CreateParty(PARTY_SESSION_TEMPLATE_NAME,
                result => OnCreatePartyCompleted(result, inviteeUserId));
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
                TriggerMessageNotification("You left the party!");
            }
        }
    }

    private void OnCreatePartyCompleted(Result<SessionV2PartySession> result, string inviteeUserId)
    {
        TriggerMessageNotification("You've just created a new party!");

        // update party data
        PartyHandler partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();
        partyHandler.SetLeaveButtonInteractable(true);
        partyHandler.currentPartyId = result.Value.id;
        partyHandler.currentLeaderUserId = result.Value.leaderId;
        UpdatePartyMembersData(result.Value.members, result.Value.leaderId);

        _partyWrapper.SendPartyInvitation(_partyWrapper.partyId, inviteeUserId, OnSendPartyInvitationCompleted);
    }

    private void OnSendPartyInvitationCompleted(Result result)
    {
        TriggerMessageNotification("You've just sent out a party invitation! Waiting for a response..");
    }

    #endregion
}