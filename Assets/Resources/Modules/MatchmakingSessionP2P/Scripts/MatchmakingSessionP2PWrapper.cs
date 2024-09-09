// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionP2PWrapper : MatchmakingSessionWrapper
{
    private string matchTicket;
    private string cachedSessionId;
    private bool isEventsListened = false;
    private bool isMatchTicketExpired = false;
    private InGameMode selectedInGameMode = InGameMode.None;
    private GameSessionServerType gameSessionServerType = GameSessionServerType.PeerToPeer;
    private bool isMatchmakingFound = false;
    private bool isJoined = false;
    private bool isInvited = false;
    protected internal event Action OnMatchmakingWithP2PStarted;
    protected internal event Action OnMatchTicketP2PCreated;
    protected internal event Action OnMatchmakingWithP2PTicketExpired;
    protected internal event Action OnMatchmakingWithP2PMatchFound;
    protected internal event Action OnMatchmakingWithP2PCanceled;
    protected internal event Action OnMatchmakingWithP2PJoinSessionStarted;
    protected internal event Action<bool /*leader status*/> OnMatchmakingWithP2PJoinSessionCompleted;
    protected internal event Action OnIntentionallyLeaveSession;
    protected internal event Action<string /*Matchmaking Error Message*/> OnMatchmakingWithP2PError;
    protected internal event Action<bool> OnDSAvailable;
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
    /// <param name="matchPool"></param>
    protected internal async void StartP2PMatchmaking(InGameMode inGameMode)
    {
        isMatchTicketExpired = false;
        selectedInGameMode = inGameMode;
        
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> sessionConfig = GameSessionConfig.SessionCreateRequest;
        
        if (!sessionConfig.TryGetValue(selectedInGameMode, out var matchTypeDict))
        {
            BytewarsLogger.LogWarning("Matchtype Not Found");
            return;
        }

        if (!matchTypeDict.TryGetValue(gameSessionServerType, out var request))
        {
            BytewarsLogger.LogWarning("SessionV2GameSessionCreateRequest Not Found");
            return;
        }

        ConnectionHandler.Initialization();
        await StartMatchmakingAsync(request.matchPool, ConnectionHandler.IsUsingLocalDS());
        GameManager.Instance.OnClientLeaveSession += OnClientLeave;
    }

    /// <summary>
    /// Cancel Matchmaking only if the user does not join to the game
    /// </summary>
    protected internal void CancelP2PMatchmaking()
    {
        if (isMatchTicketExpired)
        {
            BytewarsLogger.Log($"match ticket : {matchTicket} is expired {isMatchTicketExpired}");
            return;
        }

        CancelMatchmaking(matchTicket);
        GameManager.Instance.OnClientLeaveSession -= OnClientLeave;
    }

    #endregion


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
        BindOnInviteToGameSessionNotification();
        BindOnUserJoinedSessionNotification();

        OnMatchStarted += OnMatchStartedCallback;
        OnMatchFound += OnMatchFoundCallback;
        OnMatchExpired += OnMatchExpiredCallback;
        OnMatchTicketCreated += OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted += OnMatchTicketDeletedCallback;
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
        UnBindOnInviteToGameSessionNotification();
        UnBindOnUserJoinedSessionNotification();

        OnMatchStarted -= OnMatchStartedCallback;
        OnMatchFound -= OnMatchFoundCallback;
        OnMatchExpired -= OnMatchExpiredCallback;
        OnMatchTicketCreated -= OnMatchTicketCreatedCallback;
        OnMatchTicketDeleted -= OnMatchTicketDeletedCallback;
        OnInviteToGameSession -= OnInviteToGameSessionCallback;
        OnJoinedGameSession -= OnJoinedGameSessionCallback;
    }

    private void OnMatchStartedCallback(Result<MatchmakingV2MatchmakingStartedNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Matchmaking started");
            OnMatchmakingWithP2PStarted?.Invoke();
        }
        else
        {
            BytewarsLogger.LogWarning($"Matchmaking started");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
        }
    }

    private void OnMatchExpiredCallback(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Matchmaking Ticket is expired");
            OnMatchmakingWithP2PTicketExpired?.Invoke();
            isMatchTicketExpired = true;
        }
        else
        {
            BytewarsLogger.LogWarning($"Unable to get OnMatchticketExpired Notification from lobby");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
        }

        Reset();
    }

    private async void OnMatchFoundCallback(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Unable to get OnMatchFound Notification from lobby");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
            return;
        }

        BytewarsLogger.Log($"Match Found {result.Value.ToJsonString()}");
        isMatchmakingFound = true;
        cachedSessionId = result.Value.id;
        OnMatchmakingWithP2PMatchFound?.Invoke();

        await Task.Delay(1000);

        if (isInvited && !isJoined)
        {
            BytewarsLogger.Log("OnInvitedToSession");
            OnInvitedToSession?.Invoke();
        }
        else if (isJoined && !isInvited)
        {
            BytewarsLogger.Log("Auto-Accept Session");
            OnGetSessionDetailsCompleteEvent += OnJoiningSessionCompletedAsync;
            OnMatchmakingWithP2PJoinSessionStarted?.Invoke();
            GetGameSessionDetailsById(cachedSessionId);
        }
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
            OnMatchmakingWithP2PJoinSessionStarted?.Invoke();
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

    private void OnMatchTicketDeletedCallback()
    {
        Reset();
        OnMatchmakingWithP2PCanceled?.Invoke();
    }

    private void OnMatchTicketCreatedCallback(string matchTicketId)
    {
        matchTicket = matchTicketId;
        OnMatchTicketP2PCreated?.Invoke();
    }

    private async void StartJoinToGameSession(string sessionId)
    {
        OnJoinSessionCompleteEvent += OnJoiningSessionCompletedAsync;
        OnMatchmakingWithP2PJoinSessionStarted?.Invoke();
        await Task.Delay(1000);
        JoinSession(sessionId);
    }

    private void OnClientLeave()
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
        await Task.Delay(1000);

        if (result.IsError)
        {
            BytewarsLogger.Log($"Error : {result.Error.Message}");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
            return;
        }

        OnJoinSessionCompleteEvent -= OnJoiningSessionCompletedAsync;
        OnGetSessionDetailsCompleteEvent -= OnJoiningSessionCompletedAsync;
        BytewarsLogger.Log($"Joined to session : {cachedSessionId} - Waiting DS Status");

        bool isLeader = result.Value.leaderId == GameData.CachedPlayerState.playerId;
        OnMatchmakingWithP2PJoinSessionCompleted?.Invoke(isLeader);

        GetP2PStatus(result.Value);
    }
    

    #region MatchmakingDSEventHandler
    
    private void Reset(bool resetCache = true)
    {
        if (resetCache)
        {
            matchTicket = string.Empty;
            cachedSessionId = string.Empty;
        }

        isInvited = false;
        isJoined = false;
        isMatchmakingFound = false;
    }

    #endregion EventHandler

    #region Lobby service notification

    private async void GetP2PStatus(SessionV2GameSession session)
    {
        SessionV2DsInformation dsInfo = session.dsInformation;
            switch (dsInfo.status)
            {
                case SessionV2DsStatus.NEED_TO_REQUEST:
                    BytewarsLogger.LogWarning($"DS Status: {dsInfo.status}");
                    await Task.Delay(1000);
                    StartP2PConnection(GameData.CachedPlayerState.playerId, session);
                    UnbindMatchmakingEvent();
                    break;
            }
    }

    private void StartP2PConnection(string currentUserId, SessionV2GameSession gameSession)
    {
        if (string.IsNullOrWhiteSpace(gameSession.leaderId))
        {
            OnMatchmakingWithP2PError?.Invoke($"GameSession.id {gameSession.id} has empty/null leader id: {gameSession.leaderId}");
            Reset(false);
            return;
        }

        if (currentUserId.Equals(gameSession.leaderId))
        {
            GameData.ServerSessionID = gameSession.id;
            P2PHelper.StartAsHost(selectedInGameMode, gameSession.id);
        }
        else
        {
            P2PHelper.StartAsP2PClient(gameSession.leaderId, selectedInGameMode, gameSession.id);
        }

        Reset(false);
    }

    #endregion
}
