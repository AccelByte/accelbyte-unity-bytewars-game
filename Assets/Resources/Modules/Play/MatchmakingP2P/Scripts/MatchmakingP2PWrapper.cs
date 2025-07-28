// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Newtonsoft.Json;
using static AccelByteWarsOnlineSessionModels;
using static SessionEssentialsModels;
using static MatchmakingEssentialsModels;

public class MatchmakingP2PWrapper : MatchmakingEssentialsWrapper
{
    protected override void Awake()
    {
        base.Awake();

        Lobby.MatchmakingV2TicketExpired += (result) => OnMatchmakingExpired?.Invoke(result);
        Lobby.MatchmakingV2MatchFound += (result) => OnMatchFound?.Invoke(result);
        Lobby.SessionV2InvitedUserToGameSession += (result) => OnSessionInviteReceived?.Invoke(result);
    }

    public override void StartMatchmaking(
        string matchPool,
        ResultCallback<MatchmakingV2CreateTicketResponse> onComplete)
    {
        // If there is an existing session, leave it first.
        if (CachedSession != null)
        {
            LeaveGameSession(CachedSession.id, (leaveResult) =>
            {
                if (leaveResult.IsError)
                {
                    BytewarsLogger.LogWarning($"Failed to start matchmaking. Error {leaveResult.Error.Code}: {leaveResult.Error.Message}");
                    Result<MatchmakingV2CreateTicketResponse> errorResult = Result<MatchmakingV2CreateTicketResponse>.CreateError(leaveResult.Error);
                    OnMatchmakingStarted?.Invoke(errorResult);
                    onComplete?.Invoke(errorResult);
                    return;
                }

                StartMatchmaking(matchPool, onComplete);
            });
            return;
        }

        MatchmakingV2CreateTicketRequestOptionalParams optionalParams = new() { attributes = new() };

        // Add party session id for playing with party feature.
        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        if (partySession != null && !string.IsNullOrEmpty(partySession.id))
        {
            optionalParams.sessionId = partySession.id;
        }

        Matchmaking.CreateMatchmakingTicket(matchPool, optionalParams, (startResult) =>
        {
            // Matchmaking started successfully.
            if (!startResult.IsError)
            {
                BytewarsLogger.Log($"Success to start matchmaking. Match ticket ID: {startResult.Value.matchTicketId}");
                OnMatchmakingStarted?.Invoke(startResult);
                onComplete?.Invoke(startResult);
                return;
            }

            BytewarsLogger.LogWarning($"Failed to start matchmaking. Error {startResult.Error.Code}: {startResult.Error.Message}");

            // Attempt recovery if conflict due to existing ticket. Otherwise, return the result.
            if (startResult.Error.Code != ErrorCode.MatchmakingV2CreateMatchTicketConflict)
            {
                OnMatchmakingStarted?.Invoke(startResult);
                onComplete?.Invoke(startResult);
                return;
            }

            // Abort if the message variable is null.
            string messageVariablesJson = startResult.Error.messageVariables?.ToJsonString();
            if (string.IsNullOrEmpty(messageVariablesJson))
            {
                OnMatchmakingStarted?.Invoke(startResult);
                onComplete?.Invoke(startResult);
                return;
            }

            // Abort if the message variable does not contain conflicted match ticket attribute.
            Dictionary<string, object> messageVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageVariablesJson);
            if (messageVariables == null ||
                !messageVariables.TryGetValue(MatchTicketIdAttributeKey, out var existingMatchTicketIdObj) ||
                existingMatchTicketIdObj is not string existingMatchTicketId)
            {
                OnMatchmakingStarted?.Invoke(startResult);
                onComplete?.Invoke(startResult);
                return;
            }

            // Cancel existing ticket and retry.
            CancelMatchmaking(existingMatchTicketId, cancelResult =>
            {
                if (cancelResult.IsError)
                {
                    BytewarsLogger.LogWarning($"Failed to start matchmaking. Error {cancelResult.Error.Code}: {cancelResult.Error.Message}");
                    Result<MatchmakingV2CreateTicketResponse> errorResult = Result<MatchmakingV2CreateTicketResponse>.CreateError(cancelResult.Error);
                    OnMatchmakingStarted?.Invoke(errorResult);
                    onComplete?.Invoke(errorResult);
                    return;
                }

                StartMatchmaking(matchPool, onComplete);
            });
        });
    }

    public override void CancelMatchmaking(
        string matchTicketId,
        ResultCallback onComplete)
    {
        Matchmaking.DeleteMatchmakingTicket(matchTicketId, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Failed to cancel matchmaking with ticket ID: {matchTicketId}. " +
                    $"Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to cancel matchmaking. Match ticket ID: {matchTicketId}");
            }

            OnMatchmakingCanceled?.Invoke(result);
            onComplete?.Invoke(result);
        });
    }

    public override void JoinGameSession(
        string sessionId,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        base.JoinGameSession(sessionId, (result) =>
        {
            if (!result.IsError)
            {
                InGameMode requestedGameMode = GetGameSessionGameMode(result.Value);
                if (requestedGameMode == InGameMode.None)
                {
                    BytewarsLogger.LogWarning($"Failed to travel to the P2P host. Session's game mode is not supported by the game.");
                    onComplete?.Invoke(Result<SessionV2GameSession>.CreateError(ErrorCode.NotAcceptable, InvalidSessionTypeMessage));
                    return;
                }
                TravelToP2PHost(result.Value, requestedGameMode);
            }
            
            onComplete?.Invoke(result);
        });
    }
}
