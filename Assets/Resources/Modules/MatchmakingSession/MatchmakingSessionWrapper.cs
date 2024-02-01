// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionWrapper : GameSessionEssentialsWrapper
{
    private MatchmakingV2 _matchmakingV2;
    private static string _ticketId;

    protected internal event Action<string> OnMatchmakingFoundEvent; 
    protected internal event ResultCallback<MatchmakingV2CreateTicketResponse> OnStartMatchmakingCompleteEvent; 
    protected internal event Action OnCancelMatchmakingCompleteEvent;
    protected internal event ResultCallback<SessionV2GameJoinedNotification> OnUserJoinedGameSessionEvent;
    protected internal event Action OnMatchFoundFallbackEvent;

    protected void Awake()
    {
        base.Awake();
        
        _matchmakingV2 = MultiRegistry.GetApiClient().GetMatchmakingV2();

    }
    
    #region Matchmaking

    protected void StartMatchmaking(string matchPool, bool isLocal = false)
    {
        var optionalParams = CreateTicketRequestParams(isLocal);
        _matchmakingV2.CreateMatchmakingTicket(matchPool, optionalParams, OnStartMatchmakingComplete);
    }
    
    protected void CancelMatchmaking(string matchTicketId)
    {
        _matchmakingV2.DeleteMatchmakingTicket(matchTicketId, result => OnCancelMatchmakingComplete(result, matchTicketId));
    }

    protected void GetMatchmakingTicketDetails(string ticketId, bool isFallback = false)
    {
        _ticketId = ticketId; // cached ticket id
        if (isFallback)
        {
            GetTicketDetailsPeriodically(ticketId);
        }
        else
        {
            GetMatchmakingTicketDetailsFallback(ticketId);
        }
    }

    private void GetTicketDetailsPeriodically(string ticketId)
    {
        _matchmakingV2.GetMatchmakingTicket(ticketId, OnGetMatchmakingTicketStatusComplete);
    }

    private void GetMatchmakingTicketDetailsFallback(string ticketId)
    {
        _matchmakingV2.GetMatchmakingTicket(ticketId, OnGetMatchmakingTicketFallbackComplete);
    }
    
    #endregion

    #region EventListener
    
    protected void BindMatchmakingUserJoinedGameSession()
    {
        _lobby.SessionV2UserJoinedGameSession += OnUserJoinedGameSession;
    }
    
    protected void UnBindMatchmakingUserJoinedGameSession()
    {
        _lobby.SessionV2UserJoinedGameSession -= OnUserJoinedGameSession;
    }
    
    protected void BindOnMatchmakingStarted()
    {
        _lobby.MatchmakingV2MatchmakingStarted += OnMatchmakingStarted;
    }
    
    protected void UnBindOnMatchmakingStarted()
    {
        _lobby.MatchmakingV2MatchmakingStarted -= OnMatchmakingStarted;
    }
    
    protected void BindMatchFoundNotification()
    {
        _lobby.MatchmakingV2MatchFound += OnMatchFound;
    }
    
    protected void UnBindMatchFoundNotification()
    {
        _lobby.MatchmakingV2MatchFound -= OnMatchFound;
    }
    
    #endregion

    #region EventHandler
    
    private void OnUserJoinedGameSession(Result<SessionV2GameJoinedNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"user joined session {JsonUtility.ToJson(result.Value)}");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
        OnUserJoinedGameSessionEvent?.Invoke(result);
    }
    
    private void OnMatchmakingStarted(Result<MatchmakingV2MatchmakingStartedNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"match started {JsonUtility.ToJson(result.Value)}");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
    }
    
    private void OnMatchFound(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log(JsonUtility.ToJson(result.Value));
            OnMatchmakingFoundEvent?.Invoke(result.Value.id);
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
    }

    #endregion
    
    #region Callbacks

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"MatchTicket id {result.Value.matchTicketId}, estimated queue time {result.Value.queueTime}");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
        OnStartMatchmakingCompleteEvent?.Invoke(result);

    }

    private void OnCancelMatchmakingComplete(Result result, string ticketId)
    {
        if (!result.IsError)
        {
            OnCancelMatchmakingCompleteEvent?.Invoke();
            StopAllCoroutines();
            BytewarsLogger.Log($"success cancel matchmaking with ticket id {ticketId}");
        }
    }
    
    private void OnGetMatchmakingTicketStatusComplete(Result<MatchmakingV2MatchTicketStatus> result)
    {
        if (!result.IsError)
        {
            if (result.Value.matchFound)
            {
                OnMatchmakingFoundEvent?.Invoke(result.Value.sessionId);
            }
            else
            {
                StartCoroutine(WaitForASecond(_ticketId, GetTicketDetailsPeriodically));
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"failed to get matchmaking ticket {result.Error.Message}");
        }
    }

    private void OnGetMatchmakingTicketFallbackComplete(Result<MatchmakingV2MatchTicketStatus> result)
    {
        if (!result.IsError)
        {
            if (result.Value.matchFound)
            {
                OnMatchFoundFallbackEvent?.Invoke();
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"failed to get matchmaking ticket {result.Error.Message}");
        }
    }
    
    #endregion

    #region Utils

    private MatchmakingV2CreateTicketRequestOptionalParams CreateTicketRequestParams(bool isLocalServer)
    {
        if (!isLocalServer) return null;
        var localServerName = ConnectionHandler.LocalServerName;
        var optionalParams = new MatchmakingV2CreateTicketRequestOptionalParams
        {
            attributes = new Dictionary<string, object>()
            {
                { "server_name", localServerName },
            }
        };

        return optionalParams;
    }
    
    protected IEnumerator WaitForASecond(string ticketId, Action<string> action)
    {
        yield return new WaitForSeconds(1);
        action?.Invoke(ticketId);
    }

    #endregion
}