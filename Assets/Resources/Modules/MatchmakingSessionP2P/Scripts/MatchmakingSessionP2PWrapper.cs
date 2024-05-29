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
    private InGameMode chachedIngameMode = InGameMode.None;
    private GameSessionServerType gameSessionServerType = GameSessionServerType.PeerToPeer;
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
        chachedIngameMode = inGameMode;
        
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> sessionConfig = GameSessionConfig.SessionCreateRequest;
        
        if (!sessionConfig.TryGetValue(chachedIngameMode, out var matchTypeDict))
        {
            return;
        }

        if (!matchTypeDict.TryGetValue(gameSessionServerType, out var request))
        {
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
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Match Found {result.Value}");
            cachedSessionId = result.Value.id;
            OnMatchmakingWithP2PMatchFound?.Invoke();
            await Delay();
            StartJoinToGameSession(result.Value);
        }
        else
        {
            BytewarsLogger.LogWarning($"Unable to get OnMatchFound Notification from lobby");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
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

    private async void StartJoinToGameSession(MatchmakingV2MatchFoundNotification result)
    {
        OnJoinSessionCompleteEvent += OnJoiningSessionCompleted;
        OnMatchmakingWithP2PJoinSessionStarted?.Invoke();
        await Delay();
        JoinSession(result.id);
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
            
            if(result.Value.leaderId == GameData.CachedPlayerState.playerId)
            {
                OnMatchmakingWithP2PJoinSessionCompleted?.Invoke(true);
            } 
            else
            {
                OnMatchmakingWithP2PJoinSessionCompleted?.Invoke(false);
            }
            GetP2PStatus(result.Value);
        }
        else
        {
            BytewarsLogger.Log($"Error : {result.Error.Message}");
            OnMatchmakingWithP2PError?.Invoke(result.Error.Message);
        }

    }

    #region MatchmakingDSEventHandler
    
    private void Reset()
    {
        matchTicket = string.Empty;
        cachedSessionId = string.Empty;
    }

    private async Task Delay(int milliseconds=1000)
    {
        await Task.Delay(1000);
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
                    await Delay();
                    StartP2PConnection(GameData.CachedPlayerState.playerId, session);
                    UnbindMatchmakingEvent();
                    break;
            }
    }


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
                case SessionV2DsStatus.FAILED_TO_REQUEST:
                    BytewarsLogger.LogWarning($"DS Status: {dsInfo.status}");
                    await Delay();
                    UnbindMatchmakingEvent();
                    StartP2PConnection(GameData.CachedPlayerState.playerId, session);
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
            OnMatchmakingWithP2PError?.Invoke($"Error: {result.Error.Message}");
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }
    }

    private void StartP2PConnection(string currentUserId, SessionV2GameSession gameSession)
    {
        var leaderUserId = gameSession.leaderId;
        if (!String.IsNullOrWhiteSpace(leaderUserId))
        {
            if (currentUserId.Equals(leaderUserId))
            {
                GameData.ServerSessionID = gameSession.id;
                P2PHelper.StartAsHost(chachedIngameMode, gameSession.id);
            }
            else
            {
                P2PHelper.StartAsP2PClient(leaderUserId, chachedIngameMode, gameSession.id);
            }
        }
        else
        {
            OnMatchmakingWithP2PError?.Invoke($"GameSession.id {gameSession.id} has empty/null leader id: {gameSession.leaderId}");
        }
    }


    
    #endregion
}
