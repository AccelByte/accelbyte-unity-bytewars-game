// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class MatchmakingSessionWrapper : GameSessionUtilityWrapper
{
    #region AGS Game SDK Reference
    private MatchmakingV2 matchmakingV2;
    #endregion

    private List<SessionV2GameSession> playerSessions = new List<SessionV2GameSession>();
    private bool isGetActiveSessionCompleted = false;

    #region Matchmaking Events
    public event ResultCallback<MatchmakingV2MatchmakingStartedNotification> OnMatchStarted;
    public event ResultCallback<MatchmakingV2MatchFoundNotification> OnMatchFound;
    public event ResultCallback<SessionV2DsStatusUpdatedNotification> OnDSStatusUpdate;
    public event ResultCallback<MatchmakingV2TicketExpiredNotification> OnMatchExpired;
    public event ResultCallback<SessionV2GameInvitationNotification> OnInviteToGameSession;
    public event ResultCallback<SessionV2GameJoinedNotification> OnJoinedGameSession;
    public event Action<string /*match ticket id*/> OnMatchTicketCreated;
    public event Action<string> OnMatchmakingError;
    public event Action OnMatchTicketDeleted;
    #endregion

    protected void Awake()
    {
        base.Awake();
        matchmakingV2 = AccelByteSDK.GetClientRegistry().GetApi().GetMatchmakingV2();
    }

    #region Matchmaking

    /// <summary>
    /// Start Matchmaking by matchpool
    /// </summary>
    /// <param name="matchPool"></param>
    /// <param name="isLocal"></param>
    public async UniTask StartMatchmakingAsync(string matchPool, bool isLocal = false)
    {
        CheckActiveGameSessionClient();

        await WaitCheckActiveGameSessionAsync();

        if (playerSessions.Count > 0)
        {
            BytewarsLogger.Log($"Player is already in a session: {playerSessions.Count}");
            OnMatchmakingError?.Invoke($"Player is already in a session \n cannot start matchmaking");
            isGetActiveSessionCompleted = false;
            playerSessions.Clear();
            return;
        }

        MatchmakingV2CreateTicketRequestOptionalParams optionalParams = new MatchmakingV2CreateTicketRequestOptionalParams();
        optionalParams.attributes = new Dictionary<string, object>();

        // Add local server name.
        if (isLocal)
        {
            optionalParams.attributes.Add("server_name", ConnectionHandler.LocalServerName);
        }
        
        // Add client version
        optionalParams.attributes.Add("client_version", TutorialModuleUtil.IsOverrideDedicatedServerVersion() ? Application.version : string.Empty);

        // Add preferred regions.
        Dictionary<string, int> preferredRegions = RegionPreferencesHelper.GetEnabledRegions().ToDictionary(x => x.RegionCode, y => (int)y.Latency);
        if (preferredRegions.Count > 0)
        {
            optionalParams.latencies = preferredRegions;
        }

        // Play with Party additional code
        if (!string.IsNullOrEmpty(PartyHelper.CurrentPartyId))
        {
            optionalParams.sessionId = PartyHelper.CurrentPartyId;
        }

        matchmakingV2.CreateMatchmakingTicket(matchPool, optionalParams, OnStartMatchmakingComplete);
    }

    protected internal void CancelMatchmaking(string matchTicketId)
    {
        matchmakingV2.DeleteMatchmakingTicket(matchTicketId, result => OnCancelMatchmakingComplete(result, matchTicketId));
    }

    #endregion

    #region Lobby Service EventListener

    protected void BindMatchmakingStartedNotification()
    {
        lobby.MatchmakingV2MatchmakingStarted += OnMatchmakingStarted;
    }

    protected void UnBindMatchmakingStartedNotification()
    {
        lobby.MatchmakingV2MatchmakingStarted -= OnMatchmakingStarted;
    }

    protected void BindMatchmakingExpiredNotification()
    {
        lobby.MatchmakingV2TicketExpired += OnMatchTicketExpired;
    }

    protected void UnBindMatchmakingExpiredNotification()
    {
        lobby.MatchmakingV2TicketExpired -= OnMatchTicketExpired;
    }

    protected void BindMatchFoundNotification()
    {
        lobby.MatchmakingV2MatchFound += OnMatchmakingFound;
    }

    protected void UnBindMatchFoundNotification()
    {
        lobby.MatchmakingV2MatchFound -= OnMatchmakingFound;
    }

    protected void BindOnDSUpdateNotification()
    {
        lobby.SessionV2DsStatusChanged += OnDSStatusUpdated;
    }

    protected void UnBindOnDSUpdateNotification()
    {
        lobby.SessionV2DsStatusChanged -= OnDSStatusUpdated;
    }

    protected void BindOnInviteToGameSessionNotification()
    {
        lobby.SessionV2InvitedUserToGameSession += OnInviteUserToGameSession;
    }

    protected void UnBindOnInviteToGameSessionNotification()
    {
        lobby.SessionV2InvitedUserToGameSession -= OnInviteUserToGameSession;
    }

    protected void BindOnUserJoinedSessionNotification()
    {
        lobby.SessionV2UserJoinedGameSession += OnUserJoinedGameSession;
    }

    protected void UnBindOnUserJoinedSessionNotification()
    {
        lobby.SessionV2UserJoinedGameSession -= OnUserJoinedGameSession;
    }

    #endregion

    #region Lobby Service EventHandler

    private void OnMatchmakingStarted(Result<MatchmakingV2MatchmakingStartedNotification> result)
    {
        OnMatchStarted?.Invoke(result);
    }

    private void OnMatchTicketExpired(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        OnMatchExpired?.Invoke(result);
    }

    private void OnMatchmakingFound(Result<MatchmakingV2MatchFoundNotification> result)
    {
        OnMatchFound?.Invoke(result);
    }

    private void OnDSStatusUpdated(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        OnDSStatusUpdate?.Invoke(result);
    }

    private void OnInviteUserToGameSession(Result<SessionV2GameInvitationNotification> result)
    {
        OnInviteToGameSession?.Invoke(result);
    }

    private void OnUserJoinedGameSession(Result<SessionV2GameJoinedNotification> result)
    {
        OnJoinedGameSession?.Invoke(result);
    }

    #endregion

    #region Matchmaking Callbacks

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"MatchTicket id {result.Value.matchTicketId} Created, queue {result.Value.queueTime}");
            OnMatchTicketCreated?.Invoke(result.Value.matchTicketId);
        }
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            if (result.Error.Code == ErrorCode.MatchmakingV2CreateMatchTicketConflict)
            {
                Dictionary<string, object> messageVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Error.messageVariables.ToJsonString());
                if (messageVariables.TryGetValue("ticketID", out object ticketId))
                {
                    DeleteExistingMatchTickets(ticketId.ToString());
                }
            }
            OnMatchmakingError?.Invoke(result.Error.Message);
        }
    }

    private void OnCancelMatchmakingComplete(Result result, string ticketId)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully cancel matchmaking with ticket id {ticketId}");
            OnMatchTicketDeleted.Invoke();
        } 
        else
        {
            BytewarsLogger.Log($"Unable cancel matchmaking with ticket id {ticketId}");
        }
    }

    #endregion

    #region Utils
    private void CheckActiveGameSessionClient()
    {
        SessionV2StatusFilter filter = SessionV2StatusFilter.JOINED;
        SessionV2AttributeOrderBy orderBy = SessionV2AttributeOrderBy.createdAt;
        
        session.GetUserGameSessions(filter, orderBy, true,  result => 
        {
            if (!result.IsError)
            {
                playerSessions.AddRange(result.Value.data);
                isGetActiveSessionCompleted = true;
            } 
            else
            {
                BytewarsLogger.LogWarning($"Cannot get user's session : {result.Error.Message}");
            }
        });
    }

    protected async UniTask WaitCheckActiveGameSessionAsync()
    {
        while (isGetActiveSessionCompleted == false)
        {
            await UniTask.Yield();
        }
    }
    
    private void DeleteExistingMatchTickets(string matchTicketId)
    {
        matchmakingV2.DeleteMatchmakingTicket(matchTicketId, result => 
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully delete match ticket: {matchTicketId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"Cannot delete match ticket : {result.Error.Message}");
            }
        });
    }

    #endregion
}