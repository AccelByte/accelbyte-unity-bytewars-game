// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System.Linq;
using System.Collections.Generic;

public class PartyEssentialsWrapper : SessionEssentialsWrapper
{
    #region Party Action Button Helper
    public void OnInviteToPartyButtonClicked(string inviteeUserId)
    {
        SendPartyInvite(inviteeUserId);
    }

    public void OnKickPlayerFromPartyButtonClicked(string targetUserId)
    {
        KickPlayerFromParty(targetUserId);
    }

    public void OnPromotePartyLeaderButtonClicked(string targetUserId)
    {
        PromotePartyLeader(targetUserId);
    }
    #endregion

    private void OnEnable()
    {
        PartyEssentialsModels.PartyHelper.Initialize(this);

        Lobby.SessionV2InvitedUserToParty += OnPartyInviteReceived;
        Lobby.SessionV2UserRejectedPartyInvitation += OnPartyInviteRejected;
        Lobby.SessionV2UserKickedFromParty += OnKickedFromParty;
        Lobby.SessionV2PartyMemberChanged += OnPartyMemberChanged;
        Lobby.SessionV2PartyUpdated += OnPartyUpdated;
    }

    private void OnDisable()
    {
        PartyEssentialsModels.PartyHelper.Deinitialize();

        Lobby.SessionV2InvitedUserToParty -= OnPartyInviteReceived;
        Lobby.SessionV2UserRejectedPartyInvitation -= OnPartyInviteRejected;
        Lobby.SessionV2UserKickedFromParty -= OnKickedFromParty;
        Lobby.SessionV2PartyMemberChanged -= OnPartyMemberChanged;
        Lobby.SessionV2PartyUpdated -= OnPartyUpdated;
    }

    public void CreateParty(ResultCallback<SessionV2PartySession> onComplete = null)
    {
        SessionV2PartySessionCreateRequest request = new SessionV2PartySessionCreateRequest()
        {
            configurationName = PartyEssentialsModels.PartySessionTemplateName,
            joinability = SessionV2Joinability.INVITE_ONLY
        };

        Session.CreateParty(request, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to create party. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                CachedParty = result.Value;
                BytewarsLogger.Log($"Success to create party. Party id: {CachedParty.id}");
                OnPartySessionUpdated?.Invoke();
            }
            
            onComplete?.Invoke(result);
        });
    }

    public void LeaveParty(ResultCallback onComplete = null)
    {
        if (CachedParty == null)
        {
            const string errorMessage = "Failed to leave party. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        Session.LeaveParty(CachedParty.id, (Result result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to leave party. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.Log($"Success to leave party. Party id: {CachedParty.id}");
                CachedParty = null;
                AccelByteWarsOnlineSession.OnPartySessionUpdated?.Invoke();
            }
            
            onComplete?.Invoke(result);
        });
    }

    public void SendPartyInvite(string inviteeUserId, ResultCallback onComplete = null) 
    {
        // If not in any party, create a new one first.
        if (CachedParty == null) 
        {
            CreateParty((Result<SessionV2PartySession> result) => 
            {
                if (result.IsError) 
                {
                    BytewarsLogger.LogWarning($"Cannot send a party invitation. Failed to create a new party.");
                    onComplete?.Invoke(Result.CreateError(result.Error));
                    return;
                }

                SendPartyInvite(inviteeUserId, onComplete);
            });
            return;
        }

        Session.InviteUserToParty(CachedParty.id, inviteeUserId, (Result result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to send a party invitation. Invitee user ID: {inviteeUserId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.Log($"Success to send a party invitation. Invitee user ID: {inviteeUserId}.");
            }

            // Display push notification.
            MenuManager.Instance.PushNotification(new PushNotificationModel 
            {
                Message = result.IsError ? PartyEssentialsModels.FailedSendPartyInviteMessage : PartyEssentialsModels.SuccessSendPartyInviteMessage
            });
            
            onComplete?.Invoke(result);
        });
    }

    public void JoinParty(string partyId, ResultCallback<SessionV2PartySession> onComplete = null) 
    {
        // Leave current party first.
        if (CachedParty != null) 
        {
            LeaveParty((Result result) =>
            {
                if (result.IsError) 
                {
                    BytewarsLogger.LogWarning($"Cannot join a new party. Failed to leave current party. Error {result.Error.Code}: {result.Error.Message}");
                    onComplete.Invoke(Result<SessionV2PartySession>.CreateError(result.Error));
                    return;
                }

                JoinParty(partyId, onComplete);
            });
            return;
        }

        Session.JoinParty(partyId, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to join party. Party Id: {partyId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                CachedParty = result.Value;
                BytewarsLogger.Log($"Success to join party. Party id: {CachedParty.id}");
            }

            onComplete?.Invoke(result);
        });
    }

    public void RejectPartyInvite(string partyId, ResultCallback onComplete = null) 
    {
        Session.RejectPartyInvitation(partyId, (Result result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to reject a party invitation. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to reject a party invitation.");
            }

            onComplete?.Invoke(result);
        });
    }

    public void KickPlayerFromParty(string targetUserId, ResultCallback<SessionV2PartySessionKickResponse> onComplete = null) 
    {
        if (CachedParty == null)
        {
            const string errorMessage = "Failed to kick player from party. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<SessionV2PartySessionKickResponse>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        Session.KickUserFromParty(CachedParty.id, targetUserId, (Result<SessionV2PartySessionKickResponse> result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to kick player from party. Target user: {targetUserId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.LogWarning($"Success to kick player from party. Target user: {targetUserId}");
            }

            onComplete?.Invoke(result);
        });
    }

    public void PromotePartyLeader(string targetUserId, ResultCallback<SessionV2PartySession> onComplete = null) 
    {
        if (CachedParty == null)
        {
            const string errorMessage = "Failed to promote a new party leader. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<SessionV2PartySession>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        Session.PromoteUserToPartyLeader(CachedParty.id, targetUserId, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to promote new party leader. Target user id: {targetUserId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to promote a new party leader. New leader user id: {CachedParty.leaderId}");
            }

            onComplete?.Invoke(result);
        });
    }

    public void GetPartyDetails(ResultCallback<PartyEssentialsModels.PartyDetailsModel> onComplete = null) 
    {
        if (CachedParty == null) 
        {
            const string errorMessage = "Failed to get party details. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        Session.GetPartyDetails(CachedParty.id, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError) 
            {
                string errorMessage = $"Failed to get party details. Error {result.Error.Code}: {result.Error.Message}";
                BytewarsLogger.LogWarning(errorMessage);
                onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
                return;
            }

            SessionV2PartySession partySession = result.Value;
            string[] memberIds = result.Value.members.Where(x => x.StatusV2 == SessionV2MemberStatus.JOINED).Select(x => x.id).ToArray();
            if (memberIds.Length <= 0)
            {
                CachedParty = null;
                string errorMessage = $"Failed to get party details. No party active party members.";
                BytewarsLogger.LogWarning(errorMessage);
                onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
                return;
            }

            User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", memberIds, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
            {
                if (userDataResult.IsError)
                {
                    string errorMessage = $"Failed to get party details. Error {userDataResult.Error.Code}: {userDataResult.Error.Message}";
                    BytewarsLogger.LogWarning(errorMessage);
                    onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
                    return;
                }

                CachedParty = partySession;
                BytewarsLogger.Log($"Success to get party details. Party id: {CachedParty.id}");

                PartyEssentialsModels.PartyDetailsModel partyDetails = new PartyEssentialsModels.PartyDetailsModel
                {
                    PartySession = CachedParty,
                    MemberUserInfos = userDataResult.Value.Data
                };
                onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateOk(partyDetails));
            });
        });
    }

    private void OnPartyInviteReceived(Result<SessionV2PartyInvitationNotification> result)
    {
        if (result.IsError) 
        {
            BytewarsLogger.LogWarning($"Failed to handle received party invitation. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        string senderId = result.Value.senderId;
        string partyId = result.Value.partyId;

        BytewarsLogger.Log($"Receives party invitation from {result.Value.senderId}");

        // Display push notification.
        User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { senderId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
        {
            AccountUserPlatformData senderInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];

            string senderName = userDataResult.IsError ? 
                AccelByteWarsUtility.GetDefaultDisplayNameByUserId(senderId) : 
                AccelByteWarsOnlineUtility.GetDisplayName(senderInfo);
            string senderAvatarUrl = userDataResult.IsError ? string.Empty : senderInfo.AvatarUrl;

            MenuManager.Instance.PushNotification(new PushNotificationModel
            {
                Message = $"{senderName} {PartyEssentialsModels.PartyInviteReceivedMessage}",
                IconUrl = senderAvatarUrl,
                UseDefaultIconOnEmpty = true,
                ActionButtonTexts = new string[] { PartyEssentialsModels.AcceptPartyInviteMessage, PartyEssentialsModels.RejectPartyInviteMessage },
                ActionButtonCallback = (PushNotificationActionResult actionResult) =>
                {
                    switch (actionResult)
                    {
                        // Show accept party invitation confirmation.
                        case PushNotificationActionResult.Button1:
                            DisplayJoinPartyConfirmation(result.Value);
                            break;
                        // Reject party invitation.
                        case PushNotificationActionResult.Button2:
                            RejectPartyInvite(result.Value.partyId);
                            break;
                    }
                }
            });
        });
    }

    private void OnKickedFromParty(Result<SessionV2PartyUserKickedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle on-kicked from party event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Kicked from party. Party id: {result.Value.partyId}");
        CachedParty = null;

        // Display push notification.
        MenuManager.Instance.PushNotification(new PushNotificationModel
        {
            Message = $"You are {PartyEssentialsModels.KickedFromPartyMessage}"
        });

        AccelByteWarsOnlineSession.OnPartySessionUpdated?.Invoke();
    }

    private void OnPartyInviteRejected(Result<SessionV2PartyInvitationRejectedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle on-party invitation rejected event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        string rejecterId = result.Value.rejectedId;
        BytewarsLogger.Log($"Party invitation is rejected by user: {rejecterId}");

        // Display push notification.
        User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { rejecterId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
        {
            AccountUserPlatformData rejecterInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];
            
            string rejecterName = userDataResult.IsError ?
                AccelByteWarsUtility.GetDefaultDisplayNameByUserId(rejecterId) :
                AccelByteWarsOnlineUtility.GetDisplayName(rejecterInfo);
            string rejecterAvatarUrl = userDataResult.IsError ? string.Empty : rejecterInfo.AvatarUrl;

            MenuManager.Instance.PushNotification(new PushNotificationModel
            {
                Message = $"{rejecterName} {PartyEssentialsModels.PartyInviteRejectedMessage}",
                IconUrl = rejecterAvatarUrl,
                UseDefaultIconOnEmpty = true
            });
        });
    }

    private void OnPartyUpdated(Result<SessionV2PartySessionUpdatedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle on-party updated event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log("Party update received.");
        SessionV2PartySessionUpdatedNotification partyUpdateNotif = result.Value;

        // Display push notification regarding new party leader.
        if (CachedParty.leaderId != partyUpdateNotif.leaderId) 
        {
            string newLeaderId = partyUpdateNotif.leaderId;

            User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { newLeaderId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
            {
                AccountUserPlatformData newLeaderInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];

                string leaderName = userDataResult.IsError ?
                    AccelByteWarsUtility.GetDefaultDisplayNameByUserId(newLeaderId) :
                    AccelByteWarsOnlineUtility.GetDisplayName(newLeaderInfo);
                string leaderAvatarUrl = userDataResult.IsError ? string.Empty : newLeaderInfo.AvatarUrl;

                MenuManager.Instance.PushNotification(new PushNotificationModel
                {
                    Message = $"{leaderName} {PartyEssentialsModels.PartyNewLeaderMessage}",
                    IconUrl = leaderAvatarUrl,
                    UseDefaultIconOnEmpty = true
                });
            });
        }

        // Update cached party session data.
        CachedParty.id = partyUpdateNotif.id;
        CachedParty.namespace_ = partyUpdateNotif.namespace_;
        CachedParty.members = partyUpdateNotif.members;
        CachedParty.attributes = partyUpdateNotif.attributes;
        CachedParty.createdAt = partyUpdateNotif.createdAt;
        CachedParty.updatedAt = partyUpdateNotif.updatedAt;
        CachedParty.configuration = partyUpdateNotif.configuration;
        CachedParty.version = partyUpdateNotif.version;
        CachedParty.leaderId = partyUpdateNotif.leaderId;
        CachedParty.createdBy = partyUpdateNotif.createdBy;

        AccelByteWarsOnlineSession.OnPartySessionUpdated?.Invoke();
    }

    private void OnPartyMemberChanged(Result<SessionV2PartyMembersChangedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle on-party member changed event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        // Update cached party session data.
        SessionV2PartyMembersChangedNotification changeNotif = result.Value;
        CachedParty = changeNotif.session;

        // Collect changed member status and their user ids.
        Dictionary<string, SessionV2MemberStatus> updatedMemberStatus = new Dictionary<string, SessionV2MemberStatus>();
        if (changeNotif.joinerId != null) 
        {
            updatedMemberStatus.TryAdd(changeNotif.joinerId, SessionV2MemberStatus.JOINED);
        }
        if (changeNotif.ImpactedUserIds != null) 
        {
            if (changeNotif.ImpactedUserIds.LeftUserIds != null)
            {
                foreach (string leftMember in changeNotif.ImpactedUserIds.LeftUserIds)
                {
                    updatedMemberStatus.TryAdd(leftMember, SessionV2MemberStatus.LEFT);
                }
            }
            if (changeNotif.ImpactedUserIds.KickedUserIds != null)
            {
                foreach (string kickedMember in changeNotif.ImpactedUserIds.KickedUserIds)
                {
                    updatedMemberStatus.TryAdd(kickedMember, SessionV2MemberStatus.KICKED);
                }
            }
        }

        // Query user information and display the push notification based on member status.
        if (updatedMemberStatus.Count > 0) 
        {
            User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", updatedMemberStatus.Keys.ToArray(), (Result<AccountUserPlatformInfosResponse> userDataResult) =>
            {
                if (userDataResult.IsError)
                {
                    BytewarsLogger.LogWarning(
                        $"Failed to handle on-party member changed event. " +
                        $"Error {userDataResult.Error.Code}: {userDataResult.Error.Message}");
                    return;
                }

                foreach (AccountUserPlatformData memberInfo in userDataResult.Value.Data)
                {
                    if (!updatedMemberStatus.ContainsKey(memberInfo.UserId))
                    {
                        continue;
                    }

                    string memberName = AccelByteWarsOnlineUtility.GetDisplayName(memberInfo);
                    string pushNotifMessage = string.Empty;
                    switch (updatedMemberStatus[memberInfo.UserId])
                    {
                        case SessionV2MemberStatus.JOINED:
                            pushNotifMessage = $"{memberName} {PartyEssentialsModels.PartyMemberJoinedMessage}";
                            break;
                        case SessionV2MemberStatus.LEFT:
                            pushNotifMessage = $"{memberName} {PartyEssentialsModels.PartyMemberLeftMessage}";
                            break;
                        case SessionV2MemberStatus.KICKED:
                            pushNotifMessage = $"{memberName} {PartyEssentialsModels.KickedFromPartyMessage}";
                            break;
                    }

                    MenuManager.Instance.PushNotification(new PushNotificationModel
                    {
                        Message = pushNotifMessage,
                        IconUrl = memberInfo.AvatarUrl,
                        UseDefaultIconOnEmpty = true
                    });
                }
            });
        }

        AccelByteWarsOnlineSession.OnPartySessionUpdated?.Invoke();
    }

    private void DisplayJoinPartyConfirmation(SessionV2PartyInvitationNotification partyInvite) 
    {
        // Join the party if not in any party yet.
        bool isAloneInParty = CachedParty == null || CachedParty?.members?.Where(m => m.StatusV2 == SessionV2MemberStatus.JOINED).ToArray().Length <= 1;
        if (isAloneInParty) 
        {
            JoinParty(partyInvite.partyId);
            return;
        }

        // Show confirmation to leave current party and join the new party.
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            PartyEssentialsModels.PartyPopUpMessage,
            PartyEssentialsModels.JoinNewPartyConfirmationMessage,
            PartyEssentialsModels.RejectPartyInviteMessage,
            () => { RejectPartyInvite(partyInvite.partyId); },
            PartyEssentialsModels.AcceptPartyInviteMessage,
            () => { JoinParty(partyInvite.partyId); });
    }
}
