// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyHelper : MonoBehaviour
{
    [SerializeField] private GameObject partyInvitationPrefab;
    [SerializeField] private GameObject messageNotificationPrefab;

    private PartyEssentialsWrapper _partyWrapper;
    private AuthEssentialsWrapper _authWrapper;
    private PartyHandler _partyHandler;

    public static string CurrentPartyId = "";
    public static string CurrentLeaderUserId = "";
    public static List<PartyMemberData> PartyMembersData = new List<PartyMemberData>();

    private const string PartySessionTemplateName = "unity-party";
    private const string DefaultDisplayName = "Player-";

    private void Start()
    {
        partyInvitationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PartyInvitationEntryPanel) as GameObject;
        messageNotificationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.MessageNotificationEntryPanel) as GameObject;

        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        _partyHandler = MenuManager.Instance.GetChildComponent<PartyHandler>();

        PartyEssentialsWrapper.OnPartyInvitationReceived += OnInvitedToParty;
        PartyEssentialsWrapper.OnUserKicked += OnKickedFromParty;
        PartyEssentialsWrapper.OnPartyUpdated += CheckPartyMemberStatus;

        LoginHandler.OnLoginComplete += data => CheckPartyStatus();
    }

    public static void ResetPartyData()
    {
        CurrentPartyId = "";
        CurrentLeaderUserId = "";
        PartyMembersData.Clear();
    }

    public void TriggerMessageNotification(string messageText)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        MessageNotificationEntryPanel messageNotificationPanel = notificationHandler.AddNotificationItem<MessageNotificationEntryPanel>(messageNotificationPrefab);
        messageNotificationPanel.ChangeMessageText(messageText);
    }

    private void UpdatePartyMembersData(SessionV2MemberData[] members, string leaderId = null)
    {
        // set current party's leader id
        if (leaderId != "")
        {
            CurrentLeaderUserId = leaderId;
        }

        // get members' user info data
        string[] membersUserId = members.Select(member => member.id).ToArray();
        _authWrapper.BulkGetUserInfo(membersUserId, result => OnBulkGetUserInfo(result, membersUserId.ToList()));
    }

    #region Lobby Notification Callback Functions

    private void OnInvitedToParty(SessionV2PartyInvitationNotification invitation)
    {
        _authWrapper.GetUserByUserId(invitation.senderId, userResult =>
        {
            _authWrapper.GetUserAvatar(
                invitation.senderId,
                avatarResult => OnGetUserAvatarCompleted(avatarResult, invitation.partyId, userResult.Value.displayName));
        });
    }

    private void OnGetUserAvatarCompleted(Result<Texture2D> result, string partyId, string senderDisplayName)
    {
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        PartyInvitationEntryPanel invitationEntryPanel = notificationHandler.AddNotificationItem<PartyInvitationEntryPanel>(partyInvitationPrefab);
        invitationEntryPanel.UpdatePartyInvitationInfo(partyId, senderDisplayName, result.Value);
    }

    private void OnKickedFromParty(string partyId)
    {
        _partyHandler.HandleNotInParty();
        TriggerMessageNotification("You've been kicked from party!");
    }

    private void CheckPartyMemberStatus(string leaderId, SessionV2MemberData[] membersData)
    {
        SessionV2MemberStatus[] ignoredStatus =
        {
            SessionV2MemberStatus.LEFT, SessionV2MemberStatus.KICKED, SessionV2MemberStatus.INVITED,
            SessionV2MemberStatus.REJECTED
        };

        List<SessionV2MemberData> members = membersData.ToList();
        foreach (SessionV2MemberData member in membersData)
        {
            if (ignoredStatus.Contains(member.status))
            {
                members.Remove(member);

                // skip update party data if the current logged-in player's status is part of ignored status
                if (member.id == _authWrapper.UserData.user_id) return;
            }
        }
        UpdatePartyMembersData(members.ToArray(), leaderId);
    }

    #endregion

    #region Party Service Helper Functions

    private void CheckPartyStatus()
    {
        _partyWrapper.GetUserParties(OnGetUserPartiesCompleted);
    }

    public void PromoteToPartyLeader(string userId)
    {
        _partyWrapper.PromoteMemberToPartyLeader(_partyWrapper.PartyId, userId, OnPromoteToPartyLeaderCompleted);
    }

    public void KickFromParty(string userId)
    {
        _partyWrapper.KickMemberFromParty(_partyWrapper.PartyId, userId, OnKickMemberFromPartyCompleted);
    }

    public void InviteToParty(string inviteeUserId)
    {
        if (string.IsNullOrEmpty(_partyWrapper.PartyId))
        {
            _partyWrapper.CreateParty(PartySessionTemplateName, result => OnCreatePartyCompleted(result, inviteeUserId));
        }
        else
        {
            _partyWrapper.SendPartyInvitation(_partyWrapper.PartyId, inviteeUserId, OnSendPartyInvitationCompleted);
        }
    }

    public void HandleJoiningParty(SessionV2PartySession partySession)
    {
        CurrentPartyId = partySession.id;
        CurrentLeaderUserId = partySession.leaderId;
        _partyHandler.SetLeaveButtonInteractable(true);
        UpdatePartyMembersData(partySession.members, partySession.leaderId);
    }

    #endregion

    #region Party Service Callback Functions

    private void OnBulkGetUserInfo(Result<ListBulkUserInfoResponse> result, List<string> membersUserId)
    {
        if (!result.IsError)
        {
            List<PartyMemberData> partyData = new List<PartyMemberData>();
            foreach (BaseUserInfo userData in result.Value.data)
            {
                string displayName = userData.displayName == ""
                    ? DefaultDisplayName + userData.userId.Substring(0, 5)
                    : userData.displayName;

                _authWrapper.GetUserAvatar(userData.userId, avatarResult =>
                {
                    PartyMemberData memberData = new PartyMemberData(userData.userId, displayName, avatarResult.Value);
                    partyData.Add(memberData);

                    if (partyData.Count == membersUserId.Count)
                    {
                        PartyMembersData = partyData.OrderBy(data => membersUserId.IndexOf(data.UserId)).ToList();
                        if (_partyHandler.gameObject.activeSelf)
                        {
                            _partyHandler.DisplayPartyMembersData();
                        }
                    }
                });
            }
        }
    }

    private void OnPromoteToPartyLeaderCompleted(Result<SessionV2PartySession> result)
    {
        if (!result.IsError)
        {
            TriggerMessageNotification($"A member promoted to leader!");
        }
    }

    private void OnGetUserPartiesCompleted(Result<PaginatedResponse<SessionV2PartySession>> result)
    {
        if (!result.IsError)
        {
            foreach (SessionV2PartySession partySession in result.Value.data)
            {
                // by default, leave the party on reconnected
                _partyWrapper.LeaveParty(partySession.id, null);
            }
        }
    }

    private void OnKickMemberFromPartyCompleted(Result<SessionV2PartySessionKickResponse> result)
    {
        if (!result.IsError)
        {
            TriggerMessageNotification($"You have kicked a member from party!");
        }
    }

    private void OnCreatePartyCompleted(Result<SessionV2PartySession> result, string inviteeUserId)
    {
        if (!result.IsError)
        {
            TriggerMessageNotification("You've just created a new party!");

            // update party data
            _partyHandler.SetLeaveButtonInteractable(true);
            CurrentPartyId = result.Value.id;
            CurrentLeaderUserId = result.Value.leaderId;
            UpdatePartyMembersData(result.Value.members, result.Value.leaderId);

            _partyWrapper.SendPartyInvitation(_partyWrapper.PartyId, inviteeUserId, OnSendPartyInvitationCompleted);
        }
        else
        {
            TriggerMessageNotification($"Failed to create party! Error Message: {result.Error.Message}");
        }
    }

    private void OnSendPartyInvitationCompleted(Result result)
    {
        TriggerMessageNotification("You've just sent out a party invitation! Waiting for a response..");
    }

    #endregion
}