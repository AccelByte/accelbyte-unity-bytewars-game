// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionDSWrapper_Starter : MatchmakingSessionWrapper
{
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
    private string matchTicket;
    private string cachedSessionId;
    private bool isEventsListened = false;
    private bool isMatchTicketExpired = false;
    private InGameMode selectedInGameMode = InGameMode.None;
    private GameSessionServerType gameSessionServerType = GameSessionServerType.DedicatedServer;

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
        //Copy your code here
        await Task.Delay(40);
        BytewarsLogger.Log("Start Create Match Ticket Not Implemented");
    }

    /// <summary>
    /// Cancel Matchmaking only if the user does not join to the game
    /// </summary>
    protected internal void CancelDSMatchmaking()
    {
        //Copy your code here
        BytewarsLogger.Log("Cancel DS Matchmaking is not implemented");
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

    private void OnMatchFoundCallback(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Match Found {result.Value}");
            cachedSessionId = result.Value.id;
            OnMatchmakingWithDSMatchFound?.Invoke();
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
    }

    private async void StartJoinToGameSession(MatchmakingV2MatchFoundNotification result)
    {
        //Copy your code here
        BytewarsLogger.Log("Join to GameSession is not implemented");
    }

    private void OnClientLeave()
    {
        OnLeaveSessionCompleteEvent += OnLeaveSessionComplete;
        LeaveCurrentSession();
    }

    public void LeaveCurrentSession()
    {
        //Copy your code here
        BytewarsLogger.Log("Leave GameSession is not implemented");
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
            BytewarsLogger.Log($"DS Status : {result.Value.dsInformation.StatusV2}");

            if (result.Value.dsInformation.StatusV2 == SessionV2DsStatus.AVAILABLE)
            {
                TravelToDS(result.Value, selectedInGameMode);
                UnbindMatchmakingEvent();
            }
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
    }

    private async Task Delay(int milliseconds=1000)
    {
        await Task.Delay(milliseconds);
    }

    #endregion EventHandler

    #region DS notification Callback

    private async void OnDSStatusUpdateCallback(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        //Copy your code here
    }
    #endregion
}
