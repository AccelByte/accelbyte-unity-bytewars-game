// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PartyEssentialsWrapper : MonoBehaviour
{
    public SessionV2PartySession CurrentPartySession { private set; get; }

    public Action OnPartyUpdateDelegate = delegate { };

    private User userApi;
    private Session sessionApi;
    private Lobby lobbyApi;

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
        userApi = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        sessionApi = AccelByteSDK.GetClientRegistry().GetApi().GetSession();
        lobbyApi = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        PartyEssentialsModels.PartyHelper.Initialize(this);

        lobbyApi.SessionV2InvitedUserToParty += OnPartyInviteReceived;
        lobbyApi.SessionV2UserRejectedPartyInvitation += OnPartyInviteRejected;
        lobbyApi.SessionV2UserKickedFromParty += OnKickedFromParty;
        lobbyApi.SessionV2PartyMemberChanged += OnPartyMemberChanged;
        lobbyApi.SessionV2PartyUpdated += OnPartyUpdated;
    }

    private void OnDisable()
    {
        PartyEssentialsModels.PartyHelper.Deinitialize();

        lobbyApi.SessionV2InvitedUserToParty -= OnPartyInviteReceived;
        lobbyApi.SessionV2UserRejectedPartyInvitation -= OnPartyInviteRejected;
        lobbyApi.SessionV2UserKickedFromParty -= OnKickedFromParty;
        lobbyApi.SessionV2PartyMemberChanged -= OnPartyMemberChanged;
        lobbyApi.SessionV2PartyUpdated -= OnPartyUpdated;
    }

    public void CreateParty(ResultCallback<SessionV2PartySession> onComplete = null)
    {
        SessionV2PartySessionCreateRequest request = new SessionV2PartySessionCreateRequest()
        {
            configurationName = PartyEssentialsModels.PartySessionTemplateName,
            joinability = SessionV2Joinability.INVITE_ONLY
        };

        sessionApi.CreateParty(request, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to create party. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                CurrentPartySession = result.Value;
                BytewarsLogger.Log($"Success to create party. Party id: {CurrentPartySession.id}");
                OnPartyUpdateDelegate?.Invoke();
            }
            
            onComplete?.Invoke(result);
        });
    }

    public void LeaveParty(ResultCallback onComplete = null)
    {
        if (CurrentPartySession == null)
        {
            const string errorMessage = "Failed to leave party. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        sessionApi.LeaveParty(CurrentPartySession.id, (Result result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to leave party. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.Log($"Success to leave party. Party id: {CurrentPartySession.id}");
                CurrentPartySession = null;
                OnPartyUpdateDelegate?.Invoke();
            }
            
            onComplete?.Invoke(result);
        });
    }

    public void SendPartyInvite(string inviteeUserId, ResultCallback onComplete = null) 
    {
        // If not in any party, create a new one first.
        if (CurrentPartySession == null) 
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

        sessionApi.InviteUserToParty(CurrentPartySession.id, inviteeUserId, (Result result) =>
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
        if (CurrentPartySession != null) 
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

        sessionApi.JoinParty(partyId, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to join party. Party Id: {partyId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                CurrentPartySession = result.Value;
                BytewarsLogger.Log($"Success to join party. Party id: {CurrentPartySession.id}");
            }

            onComplete?.Invoke(result);
        });
    }

    public void RejectPartyInvite(string partyId, ResultCallback onComplete = null) 
    {
        sessionApi.RejectPartyInvitation(partyId, (Result result) =>
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
        if (CurrentPartySession == null)
        {
            const string errorMessage = "Failed to kick player from party. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<SessionV2PartySessionKickResponse>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        sessionApi.KickUserFromParty(CurrentPartySession.id, targetUserId, (Result<SessionV2PartySessionKickResponse> result) =>
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
        if (CurrentPartySession == null)
        {
            const string errorMessage = "Failed to promote a new party leader. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<SessionV2PartySession>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        sessionApi.PromoteUserToPartyLeader(CurrentPartySession.id, targetUserId, (Result<SessionV2PartySession> result) =>
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to promote new party leader. Target user id: {targetUserId}. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to promote a new party leader. New leader user id: {CurrentPartySession.leaderId}");
            }

            onComplete?.Invoke(result);
        });
    }

    public void GetPartyDetails(ResultCallback<PartyEssentialsModels.PartyDetailsModel> onComplete = null) 
    {
        if (CurrentPartySession == null) 
        {
            const string errorMessage = "Failed to get party details. Current party session is null.";
            BytewarsLogger.LogWarning(errorMessage);
            onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
            return;
        }

        sessionApi.GetPartyDetails(CurrentPartySession.id, (Result<SessionV2PartySession> result) =>
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
            userApi.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", memberIds, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
            {
                if (userDataResult.IsError)
                {
                    string errorMessage = $"Failed to get party details. Error {userDataResult.Error.Code}: {userDataResult.Error.Message}";
                    BytewarsLogger.LogWarning(errorMessage);
                    onComplete?.Invoke(Result<PartyEssentialsModels.PartyDetailsModel>.CreateError(ErrorCode.None, errorMessage));
                    return;
                }

                CurrentPartySession = partySession;
                BytewarsLogger.Log($"Success to get party details. Party id: {CurrentPartySession.id}");

                PartyEssentialsModels.PartyDetailsModel partyDetails = new PartyEssentialsModels.PartyDetailsModel
                {
                    PartySession = CurrentPartySession,
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
        userApi.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { senderId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
        {
            AccountUserPlatformData senderInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];

            string senderName =
                userDataResult.IsError || string.IsNullOrEmpty(senderInfo.DisplayName) ?
                AccelByteWarsUtility.GetDefaultDisplayNameByUserId(senderId) : 
                senderInfo.DisplayName;
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
        CurrentPartySession = null;

        // Display push notification.
        MenuManager.Instance.PushNotification(new PushNotificationModel
        {
            Message = $"You are {PartyEssentialsModels.KickedFromPartyMessage}"
        });

        OnPartyUpdateDelegate?.Invoke();
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
        userApi.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { rejecterId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
        {
            AccountUserPlatformData senderInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];
            
            string rejecterName = userDataResult.IsError || string.IsNullOrEmpty(senderInfo.DisplayName) ? 
                AccelByteWarsUtility.GetDefaultDisplayNameByUserId(rejecterId) : senderInfo.DisplayName;
            string rejecterAvatarUrl = userDataResult.IsError ? string.Empty : senderInfo.AvatarUrl;

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
        if (CurrentPartySession.leaderId != partyUpdateNotif.leaderId) 
        {
            string newLeaderId = partyUpdateNotif.leaderId;
            CurrentPartySession.leaderId = newLeaderId;

            userApi.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { newLeaderId }, (Result<AccountUserPlatformInfosResponse> userDataResult) =>
            {
                AccountUserPlatformData senderInfo = userDataResult.IsError ? null : userDataResult.Value.Data[0];

                string leaderName = userDataResult.IsError || string.IsNullOrEmpty(senderInfo.DisplayName) ?
                    AccelByteWarsUtility.GetDefaultDisplayNameByUserId(newLeaderId) : senderInfo.DisplayName;
                string leaderAvatarUrl = userDataResult.IsError ? string.Empty : senderInfo.AvatarUrl;

                MenuManager.Instance.PushNotification(new PushNotificationModel
                {
                    Message = $"{leaderName} {PartyEssentialsModels.PartyNewLeaderMessage}",
                    IconUrl = leaderAvatarUrl,
                    UseDefaultIconOnEmpty = true
                });
            });
        }

        OnPartyUpdateDelegate?.Invoke();
    }

    private void OnPartyMemberChanged(Result<SessionV2PartyMembersChangedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle on-party member changed event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        SessionV2PartyMembersChangedNotification changeNotif = result.Value;
        Dictionary<string, SessionV2MemberStatus> updatedMemberStatus = new Dictionary<string, SessionV2MemberStatus>();

        HashSet<SessionV2MemberData> updatedMemberList = 
            CurrentPartySession == null ? 
            new HashSet<SessionV2MemberData>() : 
            CurrentPartySession.members.ToHashSet();

        // Collect changed member status and their user ids.
        if (changeNotif.joinerId != null) 
        {
            updatedMemberStatus.TryAdd(changeNotif.joinerId, SessionV2MemberStatus.JOINED);
            updatedMemberList.Add(new SessionV2MemberData
            {
                id = changeNotif.joinerId,
                StatusV2 = SessionV2MemberStatus.JOINED
            });
        }
        if (changeNotif.ImpactedUserIds != null) 
        {
            if (changeNotif.ImpactedUserIds.LeftUserIds != null)
            {
                foreach (string leftMember in changeNotif.ImpactedUserIds.LeftUserIds)
                {
                    updatedMemberStatus.TryAdd(leftMember, SessionV2MemberStatus.LEFT);
                }
                updatedMemberList.RemoveWhere(x => changeNotif.ImpactedUserIds.LeftUserIds.Contains(x.id));
            }
            if (changeNotif.ImpactedUserIds.KickedUserIds != null)
            {
                foreach (string kickedMember in changeNotif.ImpactedUserIds.KickedUserIds)
                {
                    updatedMemberStatus.TryAdd(kickedMember, SessionV2MemberStatus.KICKED);
                }
                updatedMemberList.RemoveWhere(x => changeNotif.ImpactedUserIds.KickedUserIds.Contains(x.id));
            }
        }

        // Update the cached party session members if any.
        if (CurrentPartySession != null) 
        {
            CurrentPartySession.members = updatedMemberList.ToArray();
        }

        // Query user information and display the push notification based on member status.
        if (updatedMemberStatus.Count > 0) 
        {
            userApi.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", updatedMemberStatus.Keys.ToArray(), (Result<AccountUserPlatformInfosResponse> userDataResult) =>
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

                    string memberName = string.IsNullOrEmpty(memberInfo.DisplayName) ?
                        AccelByteWarsUtility.GetDefaultDisplayNameByUserId(memberInfo.UserId) : memberInfo.DisplayName;

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

        OnPartyUpdateDelegate?.Invoke();
    }

    private void DisplayJoinPartyConfirmation(SessionV2PartyInvitationNotification partyInvite) 
    {
        // Join the party if not in any party yet.
        bool isAloneInParty = CurrentPartySession != null && CurrentPartySession.members.Length <= 1;
        if (CurrentPartySession == null || isAloneInParty) 
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
