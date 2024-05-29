// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionDSWrapper : MatchmakingSessionWrapper
{
    private string matchTicket;
    private string cachedSessionId;
    private bool isEventsListened = false;
    private bool isMatchTicketExpired = false;
    private InGameMode chachedIngameMode = InGameMode.None;
    private GameSessionServerType gameSessionServerType = GameSessionServerType.DedicatedServer;
    protected internal event Action OnMatchmakingWithDSStarted;
    protected internal event Action OnMatchTicketDSCreated;
    protected internal event Action OnMatchmakingWithDSTicketExpired;
    protected internal event Action OnMatchmakingWithDSMatchFound;
    protected internal event Action OnMatchmakingWithDSCanceled;
    protected internal event Action OnMatchmakingWithDSJoinSessionStarted;
    protected internal event Action OnMatchmakingWithDSJoinSessionCompleted;
    protected internal event Action OnIntentionallyLeaveSession;
    protected internal event Action<string /*Matchmaking Error Message*/> OnMatchmakingWithDSError;
    protected internal event Action<bool> OnDSAvailable;
    protected internal event Action<string /*DS Error Message*/> OnDSError;

    private void Awake()
    {
        base.Awake();
    }

    #region Matchmaking

    /// <summary>
    /// Start a matchmaking
    /// </summary>
    /// <param name="matchPool"></param>
    protected internal async void StartDSMatchmaking(InGameMode inGameMode)
    {
        isMatchTicketExpired = false;
        chachedIngameMode = inGameMode;
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> sessionConfig = GameSessionConfig.SessionCreateRequest;
        
        if (!sessionConfig.TryGetValue(chachedIngameMode, out var matchTypeDict))
        {
            OnMatchmakingWithDSError.Invoke("Unable to get session configuration");
            return;
        }

        if (GConfig.IsUsingAMS())
        {
            gameSessionServerType = GameSessionServerType.DedicatedServerAMS;
        }

        if (!matchTypeDict.TryGetValue(gameSessionServerType, out var request))
        {
            OnMatchmakingWithDSError.Invoke("Unable to get matchpool");
            return;
        }

        ConnectionHandler.Initialization();
        await StartMatchmakingAsync(request.matchPool, ConnectionHandler.IsUsingLocalDS());
        GameManager.Instance.OnClientLeaveSession += OnClientLeave;
    }

    /// <summary>
    /// Cancel Matchmaking only if the user does not join to the game
    /// </summary>
    protected internal void CancelDSMatchmaking()
    {
        if (isMatchTicketExpired)
        {
            BytewarsLogger.Log($"Match ticket : {matchTicket} is expired {isMatchTicketExpired}");
            return;
        }

        CancelMatchmaking(matchTicket);
        GameManager.Instance.OnClientLeaveSession -= OnClientLeave;
    }

    #endregion Matchmaking

    /// <summary>
    /// Subscribe notification from this class and parent class
    /// </summary>
    protected internal void BindMatchmakingEvent()
    {
        if (!isEventsListened) 
        {
            RegisterMatchmakingEventListener();
            isEventsListened = true;
        }
    }

    /// <summary>
    /// Unsubscribe notification from this class and parent class
    /// </summary>
    protected internal void UnbindMatchmakingEvent()
    {
        DeRegisterMatchmakingEventListener();
        isEventsListened = false;
    }

    /// <summary>
    /// Subscribe all events from MatchmakingSessionWrapper 
    /// </summary>
    private void RegisterMatchmakingEventListener()
    {
        BindMatchmakingStartedNotification();
        BindMatchFoundNotification();
        BindMatchmakingExpiredNotification();
        BindOnDSUpdateNotification();

        OnMatchStarted += OnMatchStartedCallback;
        OnMatchFound += OnMatchFoundCallback;
        OnMatchExpired += OnMatchExpiredCallback;
        OnMatchTicketCreated += OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted += OnMatchTicketDeletedCallback;
        OnDSStatusUpdate += OnDSStatusUpdateCallback;
    }

    /// <summary>
    /// Unsubscribe all events from MatchmakingSessionWrapper 
    /// </summary>
    private void DeRegisterMatchmakingEventListener()
    {
        UnBindMatchmakingStartedNotification();
        UnBindMatchFoundNotification();
        UnBindMatchmakingExpiredNotification();
        UnBindOnDSUpdateNotification();

        OnMatchStarted -= OnMatchStartedCallback;
        OnMatchFound -= OnMatchFoundCallback;
        OnMatchExpired -= OnMatchExpiredCallback;
        OnMatchTicketCreated -= OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted -= OnMatchTicketDeletedCallback;
        OnDSStatusUpdate -= OnDSStatusUpdateCallback;
    }

    private void OnMatchStartedCallback(Result<MatchmakingV2MatchmakingStartedNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Matchmaking started");
            OnMatchmakingWithDSStarted?.Invoke();
        }
        else
        {
            BytewarsLogger.LogWarning($"Matchmaking started");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
        }

    }

    private void OnMatchExpiredCallback(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Matchmaking Ticket is expired");
            OnMatchmakingWithDSTicketExpired?.Invoke();
            isMatchTicketExpired = true;
        }
        else
        {
            BytewarsLogger.LogWarning($"Unable to get OnMatchticketExpired Notification from lobby");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
        }

        Reset();
    }

    private async void OnMatchFoundCallback(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Match Found with matchpool : {result.Value.matchPool}");
            cachedSessionId = result.Value.id;
            OnMatchmakingWithDSMatchFound?.Invoke();
            await Delay();
            StartJoinToGameSession(result.Value);
        }
        else
        {
            BytewarsLogger.LogWarning($"Unable to get OnMatchFound Notification from lobby");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
        }
    }

    private void OnMatchTicketDeletedCallback()
    {
        Reset();
        OnMatchmakingWithDSCanceled?.Invoke();
    }

    private void OnMatchTicketCreatedCallback(string matchTicketId)
    {
        matchTicket = matchTicketId;
        OnMatchTicketDSCreated?.Invoke();
    }

    private async void StartJoinToGameSession(MatchmakingV2MatchFoundNotification result)
    {
        OnJoinSessionCompleteEvent += OnJoiningSessionCompleted;
        OnMatchmakingWithDSJoinSessionStarted?.Invoke();
        await Delay();
        JoinSession(result.id);
    }

    public void OnClientLeave()
    {
        GameManager.Instance.OnClientLeaveSession -= OnClientLeave;
        OnLeaveSessionCompleteEvent += OnLeaveSessionComplete;
        LeaveCurrentSession();
    }

    public void LeaveCurrentSession()
    {
        LeaveSession(cachedSessionId);
    }

    private void OnLeaveSessionComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            OnIntentionallyLeaveSession?.Invoke();
            OnLeaveSessionCompleteEvent -= OnLeaveSessionComplete;
        }
    }

    private void OnJoiningSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            OnJoinSessionCompleteEvent -= OnJoiningSessionCompleted;
            BytewarsLogger.Log($"Joined to session : {cachedSessionId} - Waiting DS Status");
            OnMatchmakingWithDSJoinSessionCompleted?.Invoke();
        }
        else
        {
            BytewarsLogger.Log($"Error : {result.Error.Message}");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
        }

    }

    #region MatchmakingDSEventHandler
    
    private void Reset()
    {
        matchTicket = string.Empty;
        cachedSessionId = string.Empty;
        chachedIngameMode = InGameMode.None;
    }

    private async Task Delay(int milliseconds=1000)
    {
        await Task.Delay(milliseconds);
    }


    #endregion EventHandler

    #region Lobby service notification

    private async void OnDSStatusUpdateCallback(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        BytewarsLogger.Log($"{GameData.CachedPlayerState.playerId}");
        if (!result.IsError)
        {
            SessionV2GameSession session = result.Value.session;
            SessionV2DsInformation dsInfo = session.dsInformation;

            BytewarsLogger.Log($"DS Status updated: {dsInfo.status}");
            switch (dsInfo.status)
            {
                case SessionV2DsStatus.AVAILABLE:
                    await Delay();
                    OnDSAvailable?.Invoke(true);
                    await Delay();
                    TravelToDS(session, chachedIngameMode);
                    UnbindMatchmakingEvent();
                    break;
                case SessionV2DsStatus.FAILED_TO_REQUEST:
                    BytewarsLogger.LogWarning($"DS Status: {dsInfo.status}");
                    OnDSError?.Invoke($"DS Status: {dsInfo.status}");
                    UnbindMatchmakingEvent();
                    break;
                case SessionV2DsStatus.REQUESTED:
                    BytewarsLogger.LogWarning($"DS Status: {dsInfo.status}, Waiting");
                    break;
                case SessionV2DsStatus.ENDED:
                    BytewarsLogger.LogWarning($"DS Status: {dsInfo.status}, send ended notification");
                    UnbindMatchmakingEvent();
                    break;
            }
        }
        else
        {
            OnDSError?.Invoke($"Error: {result.Error.Message}");
            Debug.Log($"Error: {result.Error.Message}");
        }
    }
    #endregion
}
