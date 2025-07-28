// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;

public class SessionEssentialsWrapper : AccelByteWarsOnlineSession
{
    public override void CreateGameSession(
        SessionV2GameSessionCreateRequest request,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        // Leave the existing session before creating a new session.
        if (CachedSession != null)
        {
            LeaveGameSession(CachedSession.id, (leaveResult) =>
            {
                // Abort only if there's an error and it's not due to a missing session.
                if (leaveResult.IsError && leaveResult.Error.Code != ErrorCode.SessionIdNotFound)
                {
                    BytewarsLogger.LogWarning($"Failed to create game session. Error {leaveResult.Error.Code}: {leaveResult.Error.Message}");
                    onComplete?.Invoke(Result<SessionV2GameSession>.CreateError(leaveResult.Error));
                    return;
                }

                CreateGameSession(request, onComplete);
            });
            return;
        }

        // Create a new session.
        Session.CreateGameSession(request, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to create game session. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to create game session. Session id: {result.Value.id}");
            }

            CachedSession = result.Value;
            onComplete?.Invoke(result);
        });
    }

    public override void JoinGameSession(
        string sessionId,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        // Leave the existing session before joining a new session.
        if (CachedSession != null)
        {
            LeaveGameSession(CachedSession.id, (leaveResult) =>
            {
                // Abort only if there's an error and it's not due to a missing session.
                if (leaveResult.IsError && leaveResult.Error.Code != ErrorCode.SessionIdNotFound)
                {
                    BytewarsLogger.LogWarning($"Failed to join game session. Error {leaveResult.Error.Code}: {leaveResult.Error.Message}");
                    onComplete?.Invoke(Result<SessionV2GameSession>.CreateError(leaveResult.Error));
                    return;
                }

                JoinGameSession(sessionId, onComplete);
            });
            return;
        }

        // Join a new session.
        Session.JoinGameSession(sessionId, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to join game session. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to join game session. Session id: {result.Value.id}");
            }

            CachedSession = result.Value;
            onComplete?.Invoke(result);
        });
    }

    public override void LeaveGameSession(
        string sessionId,
        ResultCallback onComplete)
    {
        Session.LeaveGameSession(sessionId, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Failed to leave game session with ID: {sessionId}. " +
                    $"Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to leave game session. Session ID: {sessionId}");
            }

            CachedSession = null;
            onComplete?.Invoke(result);
        });
    }

    public override void SendGameSessionInvite(
        string sessionId,
        string inviteeUserId,
        ResultCallback onComplete)
    {
        Session.InviteUserToGameSession(
            sessionId,
            inviteeUserId,
            (result) =>
            {
                if (result.IsError)
                {
                    BytewarsLogger.LogWarning(
                        $"Failed to send game session invite to {inviteeUserId}. " +
                        $"Error {result.Error.Code}: {result.Error.Message}");
                }
                else
                {
                    BytewarsLogger.Log($"Success to send game session invite to {inviteeUserId}");
                }

                onComplete?.Invoke(result);
            });
    }

    public override void RejectGameSessionInvite(
        string sessionId,
        ResultCallback onComplete)
    {
        Session.RejectGameSessionInvitation(sessionId, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Failed to reject game session with ID: {sessionId}. " +
                    $"Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to reject game session. Session ID: {sessionId}");
            }

            onComplete?.Invoke(result);
        });
    }
}
