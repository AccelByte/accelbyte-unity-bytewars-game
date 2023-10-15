using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyEssentialsWrapper : MonoBehaviour
{
    private Session session;
    private Lobby lobby;

    public string partyId;

    // Start is called before the first frame update
    void Start()
    {
        session = MultiRegistry.GetApiClient().GetSession();
        lobby = MultiRegistry.GetApiClient().GetLobby();
    }
    
    #region Lobby Notifications Listener Functions

    
    
    #endregion

    #region AB Service Functions

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

    #endregion

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
            partyId = result.Value.id;
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
            partyId = result.Value.id;
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
            partyId = null;
        }
        else
        {
            Debug.Log($"Failed to leave the party session. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    #endregion
}
