// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;

public class MatchmakingSessionP2PWrapper_Starter : MatchmakingSessionWrapper
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
        //Copy your code here
    }

    /// <summary>
    /// Cancel Matchmaking only if the user does not join to the game
    /// </summary>
    protected internal void CancelP2PMatchmaking()
    {
        //Copy your code here
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
        //Copy your code here
    }

    /// <summary>
    /// Unsubscribe all events from MatchmakingSessionWrapper 
    /// </summary>
    private void DeRegisterMatchmakingEventListener()
    {
        //Copy your code here
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

        // Wait a moment to ensure the host information is received before join the game session.
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
        //Copy your code here
    }

    private void OnClientLeave()
    {
        GameManager.Instance.OnClientLeaveSession -= OnClientLeave;
        OnLeaveSessionCompleteEvent += OnLeaveSessionComplete;
        LeaveCurrentSession();
    }

    public void LeaveCurrentSession()
    {
        //Copy your code here
        BytewarsLogger.Log("Leave GameSession is not implemented");
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

    private void OnLeaveSessionComplete(Result result)
    {
        if (!result.IsError)
        {
            OnIntentionallyLeaveSession?.Invoke();
            OnLeaveSessionCompleteEvent -= OnLeaveSessionComplete;
        }
    }

    private async void OnJoiningSessionCompletedAsync(Result<SessionV2GameSession> result)
    {
        //Copy your code here
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
        //Copy your code here
    }

    private void StartP2PConnection(string currentUserId, SessionV2GameSession gameSession)
    {
        //Copy your code here
    }
    
    #endregion
}
