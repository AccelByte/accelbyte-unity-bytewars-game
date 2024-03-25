// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PlayWithPartyEssentialsWrapper : MonoBehaviour
{
    public delegate void GameSessionInvitationDelegate(string sessionId);
    public static event GameSessionInvitationDelegate OnGameSessionInvitationReceived = delegate { };

    private Session session;
    private Lobby lobby;

    private void Start()
    {
        session = AccelByteSDK.GetClientRegistry().GetApi().GetSession();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        LoginHandler.onLoginCompleted += data => SubscribeLobbyNotifications();
    }

    #region AB Service Functions

    private void SubscribeLobbyNotifications()
    {
        if (!lobby.IsConnected) lobby.Connect();
        lobby.SessionV2InvitedUserToGameSession += result => OnGameSessionInvitationReceived.Invoke(result.Value.sessionId); ;
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
            BytewarsLogger.Log($" success {result.Value.ToJsonString()}");
        }
        else
        {
            BytewarsLogger.Log($" failed {result.Error.ToJsonString()}");
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
