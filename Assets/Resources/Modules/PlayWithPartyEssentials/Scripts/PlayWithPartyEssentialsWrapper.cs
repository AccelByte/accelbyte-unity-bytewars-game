using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PlayWithPartyEssentialsWrapper : MonoBehaviour
{
    public delegate void GameSessionInvitationDelegate(string sessionId);
    public static event GameSessionInvitationDelegate onGameSessionInvitationReceived = delegate {};

    private Session session;
    private Lobby lobby;
    
    // Start is called before the first frame update
    void Start()
    {
        session = MultiRegistry.GetApiClient().GetSession();
        lobby = MultiRegistry.GetApiClient().GetLobby();

        LoginHandler.onLoginCompleted += data => SubscribeLobbyNotifications();
    }

    #region AB Service Functions
    
    private void SubscribeLobbyNotifications()
    {
        if (!lobby.IsConnected) lobby.Connect();
        lobby.SessionV2InvitedUserToGameSession += result => onGameSessionInvitationReceived.Invoke(result.Value.sessionId);;
    }
    
    public void GetGameSessionBySessionId(string sessionId, ResultCallback<SessionV2GameSession> resultCallback)
    {
        session.GetGameSessionDetailsBySessionId(
            sessionId,
            result => OnGetGameSessionBySessionIdCompleted(result, resultCallback));
    }

    public void InviteUserToGameSession(string sessionId, string inviteeId, ResultCallback resultCallback)
    {
        session.InviteUserToGameSession(
            sessionId,
            inviteeId,
            result => OnInviteUserToGameSessionCompleted(result, resultCallback)
        );
    }
    
    #endregion

    #region Callback Functions

    private void OnGetGameSessionBySessionIdCompleted(Result<SessionV2GameSession> result, ResultCallback<SessionV2GameSession> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log($"Success getting the game session");
        }
        else
        {
            Debug.Log($"Failed to get the game session. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }

    private void OnInviteUserToGameSessionCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Success inviting user to current game session!");
        }
        else
        {
            Debug.Log($"Failed to invite user to current game session. Message: {result.Error.Message}");
        }
    }
    
    #endregion
}
