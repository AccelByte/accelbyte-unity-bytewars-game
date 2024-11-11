// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MatchmakingSessionDSWrapper : MatchmakingSessionWrapper
{
    private string matchTicket;
    private string cachedSessionId;
    private bool isEventsListened = false;
    private bool isMatchTicketExpired = false;
    private InGameMode selectedInGameMode = InGameMode.None;
    private GameSessionServerType gameSessionServerType = GameSessionServerType.DedicatedServer;
    private bool isMatchmakingFound = false;
    private bool isJoined = false;
    private bool isInvited = false;
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
    protected internal event Action OnInvitedToSession;
    protected internal event Action OnUserJoinedGameSession;
    protected internal event Action OnSessionMemberUpdate;

    private void Awake()
    {
        base.Awake();
    }

    #region Matchmaking

    /// <summary>
    /// Start a matchmaking
    /// </summary>
    /// <param name="inGameMode">ingame mode enum</param>
    protected internal async void StartDSMatchmaking(InGameMode inGameMode)
    {
        isMatchTicketExpired = false;
        selectedInGameMode = inGameMode;
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> sessionConfig = GameSessionConfig.SessionCreateRequest;
        
        if (!sessionConfig.TryGetValue(selectedInGameMode, out var matchTypeDict))
        {
            BytewarsLogger.LogWarning("Matchtype Not Found");
            OnMatchmakingWithDSError.Invoke("Unable to get session configuration");
            return;
        }

        if (GConfig.IsUsingAMS())
        {
            gameSessionServerType = GameSessionServerType.DedicatedServerAMS;
        }

        if (!matchTypeDict.TryGetValue(gameSessionServerType, out var request))
        {
            BytewarsLogger.LogWarning("SessionV2GameSessionCreateRequest Not Found");
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
        BindOnInviteToGameSessionNotification();
        BindOnUserJoinedSessionNotification();

        OnMatchStarted += OnMatchStartedCallback;
        OnMatchFound += OnMatchFoundCallback;
        OnMatchExpired += OnMatchExpiredCallback;
        OnMatchTicketCreated += OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted += OnMatchTicketDeletedCallback;
        OnDSStatusUpdate += OnDSStatusUpdateCallback;
        OnInviteToGameSession += OnInviteToGameSessionCallback;
        OnJoinedGameSession += OnJoinedGameSessionCallback;
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
        UnBindOnInviteToGameSessionNotification();
        UnBindOnUserJoinedSessionNotification();

        OnMatchStarted -= OnMatchStartedCallback;
        OnMatchFound -= OnMatchFoundCallback;
        OnMatchExpired -= OnMatchExpiredCallback;
        OnMatchTicketCreated -= OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted -= OnMatchTicketDeletedCallback;
        OnDSStatusUpdate -= OnDSStatusUpdateCallback;
        OnInviteToGameSession -= OnInviteToGameSessionCallback;
        OnJoinedGameSession -= OnJoinedGameSessionCallback;
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
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Unable to get OnMatchFound Notification from lobby");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
            return;
        }

        BytewarsLogger.Log($"Match Found with matchpool : {result.Value.matchPool}");
        isMatchmakingFound = true;
        cachedSessionId = result.Value.id;
        OnMatchmakingWithDSMatchFound?.Invoke();

        await UniTask.Delay(1000);

        if (isInvited && !isJoined)
        {
            BytewarsLogger.Log("OnInvitedToSession");
            OnInvitedToSession?.Invoke();
        }
        else if (isJoined && !isInvited) 
        {
            BytewarsLogger.Log("Auto-Accept Session");
            OnGetSessionDetailsCompleteEvent += OnJoiningSessionCompletedAsync;
            OnMatchmakingWithDSJoinSessionStarted?.Invoke();
            GetGameSessionDetailsById(cachedSessionId);
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

    private void OnJoinedGameSessionCallback(Result<SessionV2GameJoinedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            return;
        }

        isJoined = true;

        if (isMatchmakingFound && !isInvited)
        {
            OnGetSessionDetailsCompleteEvent += OnJoiningSessionCompletedAsync;
            OnMatchmakingWithDSJoinSessionStarted?.Invoke();
            GetGameSessionDetailsById(cachedSessionId);
        }
    }

    private void OnInviteToGameSessionCallback(Result<SessionV2GameInvitationNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            return;
        }

        isInvited = true;
        BytewarsLogger.Log($"User is invited to game session");

        if (isMatchmakingFound && !isJoined)
        {
            OnInvitedToSession?.Invoke();
        }
    }

    private async void StartJoinToGameSession(string sessionId)
    {
        OnJoinSessionCompleteEvent += OnJoiningSessionCompletedAsync;
        OnMatchmakingWithDSJoinSessionStarted?.Invoke();
        await UniTask.Delay(1000);
        JoinSession(sessionId);
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
        Reset(true);
    }

    public void RejectSessionInvitation()
    {
        RejectSession(cachedSessionId);
        Reset();
    }

    public void AcceptSessionInvitation()
    {
        StartJoinToGameSession(cachedSessionId);
    }

    private void OnLeaveSessionComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            OnIntentionallyLeaveSession?.Invoke();
            OnLeaveSessionCompleteEvent -= OnLeaveSessionComplete;
        }
    }

    private async void OnJoiningSessionCompletedAsync(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.Log($"Error : {result.Error.Message}");
            OnMatchmakingWithDSError?.Invoke(result.Error.Message);
            return;
        }

        OnJoinSessionCompleteEvent -= OnJoiningSessionCompletedAsync;
        OnGetSessionDetailsCompleteEvent -= OnJoiningSessionCompletedAsync;
        BytewarsLogger.Log($"Joined to session : {cachedSessionId} - Waiting DS Status");
        OnMatchmakingWithDSJoinSessionCompleted?.Invoke();
        BytewarsLogger.Log($"DS Status : {result.Value.dsInformation.StatusV2}");

        if (result.Value.dsInformation.StatusV2 == SessionV2DsStatus.AVAILABLE)
        {
            OnDSAvailable?.Invoke(true);
            Reset(false);
            await UniTask.Delay(1000); // Add a delay to ensure the server-found information appears.
            TravelToDS(result.Value, selectedInGameMode);
            UnbindMatchmakingEvent();
        }
    }

    #region MatchmakingDSEventHandler
    
    private void Reset(bool resetCache = true)
    {
        if (resetCache)
        {
            matchTicket = string.Empty;
            cachedSessionId = string.Empty;
            selectedInGameMode = InGameMode.None;
        }

        isInvited = false;
        isJoined = false;
        isMatchmakingFound = false;
    }

    #endregion EventHandler

    #region DS notification Callback

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
                    OnDSAvailable?.Invoke(true);
                    await UniTask.Delay(1000); // Add a delay to ensure the server-found information appears.
                    TravelToDS(session, selectedInGameMode);
                    UnbindMatchmakingEvent();
                    break;
                case SessionV2DsStatus.FAILED_TO_REQUEST:
                    OnDSError?.Invoke($"DS Status: {dsInfo.status}");
                    UnbindMatchmakingEvent();
                    break;
                case SessionV2DsStatus.REQUESTED:
                    break;
                case SessionV2DsStatus.ENDED:
                    UnbindMatchmakingEvent();
                    break;
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            OnDSError?.Invoke($"Error: {result.Error.Message}");
        }
        Reset(false);
    }
    #endregion
}
