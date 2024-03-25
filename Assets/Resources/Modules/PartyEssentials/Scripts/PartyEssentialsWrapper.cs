// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyEssentialsWrapper : MonoBehaviour
{
    public delegate void PartyInvitationDelegate(SessionV2PartyInvitationNotification partyInvitation);
    public delegate void PartyKickedDelegate(string partyId);
    public delegate void PartyUpdateDelegate(string leaderId, SessionV2MemberData[] members);

    public static event PartyInvitationDelegate OnPartyInvitationReceived = delegate { };
    public static event PartyKickedDelegate OnUserKicked = delegate { };
    public static event PartyUpdateDelegate OnPartyUpdated = delegate { };

    private Session session;
    private Lobby lobby;

    public string PartyId;

    private void Start()
    {
        session = AccelByteSDK.GetClientRegistry().GetApi().GetSession();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        LoginHandler.onLoginCompleted += data => SubscribeLobbyNotifications();
    }

    #region AB Service Functions
    private void SubscribeLobbyNotifications()
    {
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }

        // current user related notification
        lobby.SessionV2InvitedUserToParty += result => OnPartyInvitationReceived.Invoke(result.Value);
        lobby.SessionV2UserKickedFromParty += result => OnUserKicked.Invoke(result.Value.partyId);

        // other users related notification
        lobby.SessionV2PartyUpdated += result => OnPartyUpdated.Invoke(result.Value.leaderId, result.Value.members);
        lobby.SessionV2PartyMemberChanged += result => OnPartyUpdated.Invoke(result.Value.leaderId, result.Value.session.members);
    }

    public void GetUserParties(ResultCallback<PaginatedResponse<SessionV2PartySession>> resultCallback)
    {
        session.GetUserParties(result => OnGetUserPartiesCompleted(result, resultCallback));
    }

    public void CreateParty(string SessionTemplateName, ResultCallback<SessionV2PartySession> resultCallback)
    {
        SessionV2PartySessionCreateRequest request = new SessionV2PartySessionCreateRequest()
        {
            configurationName = SessionTemplateName,
            joinability = SessionV2Joinability.INVITE_ONLY
        };

        session.CreateParty(
            request,
            result => OnCreatePartyCompleted(result, resultCallback)
        );
    }

    public void SendPartyInvitation(string partyId, string inviteeUserId, ResultCallback resultCallback)
    {
        session.InviteUserToParty(
            partyId,
            inviteeUserId,
            result => OnSendPartyInvitationCompleted(result, resultCallback)
        );
    }

    public void JoinParty(string partyId, ResultCallback<SessionV2PartySession> resultCallback)
    {
        session.JoinParty(
            partyId,
            result => OnJoinPartyCompleted(result, resultCallback)
        );
    }

    public void RejectPartyInvitation(string partyId, ResultCallback resultCallback)
    {
        session.RejectPartyInvitation(
            partyId,
            result => OnRejectPartyInvitationCompleted(result, resultCallback)
        );
    }

    public void PromoteMemberToPartyLeader(string partyId, string memberId, ResultCallback<SessionV2PartySession> resultCallback)
    {
        session.PromoteUserToPartyLeader(
            partyId,
            memberId,
            result => OnPromoteMemberToPartyLeaderCompleted(result, resultCallback)
        );
    }

    public void KickMemberFromParty(string partyId, string memberId, ResultCallback<SessionV2PartySessionKickResponse> resultCallback)
    {
        session.KickUserFromParty(
            partyId,
            memberId,
            result => OnKickMemberFromPartyCompleted(result, resultCallback)
        );
    }

    public void LeaveParty(string partyId, ResultCallback resultCallback)
    {
        session.LeaveParty(
            partyId,
            result => OnLeavePartyCompleted(result, resultCallback)
        );
    }
    #endregion AB Service Functions

    #region Callback Functions
    private void OnGetUserPartiesCompleted(Result<PaginatedResponse<SessionV2PartySession>> result, ResultCallback<PaginatedResponse<SessionV2PartySession>> customCallback)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully get the current player's parties!");
        }
        else
        {
            Debug.Log($"Failed to get the current player's parties. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnCreatePartyCompleted(Result<SessionV2PartySession> result, ResultCallback<SessionV2PartySession> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully created a party session!");
            PartyId = result.Value.id;
        }
        else
        {
            Debug.Log($"Failed to create a party session. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnSendPartyInvitationCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully sent the party invitation");
        }
        else
        {
            Debug.Log($"Failed to send the party invitation. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnJoinPartyCompleted(Result<SessionV2PartySession> result, ResultCallback<SessionV2PartySession> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully joined the party session");
            PartyId = result.Value.id;
        }
        else
        {
            Debug.Log($"Failed to join the party session. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnRejectPartyInvitationCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully rejected the party invitation");
        }
        else
        {
            Debug.Log($"Failed to reject the party invitation. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnPromoteMemberToPartyLeaderCompleted(Result<SessionV2PartySession> result, ResultCallback<SessionV2PartySession> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully promoted the member as party leader");
        }
        else
        {
            Debug.Log($"Failed to promote the member as party leader. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnKickMemberFromPartyCompleted(Result<SessionV2PartySessionKickResponse> result, ResultCallback<SessionV2PartySessionKickResponse> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully kicked the member from party session");
        }
        else
        {
            Debug.Log($"Failed to kick the member from party session. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }

    private void OnLeavePartyCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully left the party session");
            PartyId = null;
        }
        else
        {
            Debug.Log($"Failed to leave the party session. Message: {result.Error.Message}");
        }

        customCallback?.Invoke(result);
    }
    #endregion Callback Functions
}
