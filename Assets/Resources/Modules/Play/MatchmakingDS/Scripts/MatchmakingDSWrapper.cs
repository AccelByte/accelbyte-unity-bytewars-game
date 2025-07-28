// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Newtonsoft.Json;
using static AccelByteWarsOnlineSessionModels;
using static SessionEssentialsModels;
using static MatchmakingEssentialsModels;

public class MatchmakingDSWrapper : MatchmakingEssentialsWrapper
{
    protected override void Awake()
    {
        base.Awake();

        Lobby.MatchmakingV2TicketExpired += (result) => OnMatchmakingExpired?.Invoke(result);
        Lobby.MatchmakingV2MatchFound += (result) => OnMatchFound?.Invoke(result);
        Lobby.SessionV2InvitedUserToGameSession += (result) => OnSessionInviteReceived?.Invoke(result);
        Lobby.SessionV2DsStatusChanged += (result) => OnDSStatusChanged?.Invoke(result);
    }

    public override void StartMatchmaking(
        string matchPool,
        ResultCallback<MatchmakingV2CreateTicketResponse> onComplete)
    {
        // Leave the existing session before starting matchmaking.
        if (CachedSession != null)
        {
            LeaveGameSession(CachedSession.id, (leaveResult) =>
            {
                // Abort only if there's an error and it's not due to a missing session.
                if (leaveResult.IsError && leaveResult.Error.Code != ErrorCode.SessionIdNotFound)
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

        // Add local server name.
        if (string.IsNullOrEmpty(ConnectionHandler.LocalServerName))
        {
            optionalParams.attributes.Add(ServerNameAttributeKey, ConnectionHandler.LocalServerName);
        }

        // Add client version
        optionalParams.attributes.Add(ClientVersionAttributeKey, ClientVersion);

        // Add preferred regions.
        Dictionary<string, int> preferredRegions =
            RegionPreferencesModels.GetEnabledRegions().ToDictionary(x => x.RegionCode, y => (int)y.Latency);
        if (preferredRegions.Count > 0)
        {
            optionalParams.latencies = preferredRegions;
        }

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
        // Reregister delegate to listen for dedicated server status changed event.
        OnDSStatusChanged -= OnDSStatusChangedReceived;
        OnDSStatusChanged += OnDSStatusChangedReceived;

        base.JoinGameSession(sessionId, (result) =>
        {
            // If dedicated server is ready, broadcast the dedicated server status changed event.
            if (!result.IsError && result.Value.dsInformation.StatusV2 == SessionV2DsStatus.AVAILABLE)
            {
                OnDSStatusChanged?.Invoke(Result<SessionV2DsStatusUpdatedNotification>.CreateOk(new()
                {
                    session = result.Value,
                    sessionId = result.Value.id,
                    error = string.Empty
                }));
            }

            onComplete?.Invoke(result);
        });
    }

    private void OnDSStatusChangedReceived(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            OnDSStatusChanged -= OnDSStatusChangedReceived;
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        SessionV2GameSession session = result.Value.session;
        SessionV2DsInformation dsInfo = session.dsInformation;

        // Check if the requested game mode is supported.
        InGameMode requestedGameMode = GetGameSessionGameMode(session);
        if (requestedGameMode == InGameMode.None)
        {
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Session's game mode is not supported by the game.");
            OnDSStatusChanged -= OnDSStatusChangedReceived;
            OnDSStatusChanged?.Invoke(
                Result<SessionV2DsStatusUpdatedNotification>.
                CreateError(ErrorCode.NotAcceptable, InvalidSessionTypeMessage));
            return;
        }

        // Check the dedicated server status.
        switch (dsInfo.StatusV2)
        {
            case SessionV2DsStatus.AVAILABLE:
                OnDSStatusChanged -= OnDSStatusChangedReceived;
                TravelToDS(session, requestedGameMode);
                break;
            case SessionV2DsStatus.FAILED_TO_REQUEST:
            case SessionV2DsStatus.ENDED:
            case SessionV2DsStatus.UNKNOWN:
                OnDSStatusChanged -= OnDSStatusChangedReceived;
                BytewarsLogger.LogWarning(
                    $"Failed to handle dedicated server status changed event. " +
                    $"Session failed to request for dedicated server due to unknown reason.");
                break;
            default:
                BytewarsLogger.Log($"Received dedicated server status change. Status: {dsInfo.StatusV2}");
                break;
        }
    }
}
