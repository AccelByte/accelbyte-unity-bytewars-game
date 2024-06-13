// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionWrapper : GameSessionUtilityWrapper
{
    public Action OnCreateMatchCancelled;
    protected internal event Action<Result<PaginatedResponse<SessionV2GameSession>>> OnBrowseMatchSessionCompleteEvent;
    protected internal event Action OnLeaveSessionCompleted;
    protected internal Action<bool> OnCreatedMatchSession;
    protected internal Action<string> OnJoinedMatchSession;
    protected internal Action<string> OnCreateOrJoinError;
    protected internal InGameMode RequestedGameMode = InGameMode.None;
    private SessionV2GameSession gameSession;

    protected void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// Function to Create Match Session 
    /// </summary>
    /// <param name="request"></param>
    protected internal void CreateMatchSession(InGameMode inGameMode, GameSessionServerType sessionServerType)
    {
        GameManager.Instance.OnClientLeaveSession += LeaveCurrentGameSession;
        lobby.SessionV2GameSessionMemberChanged += OnV2GameSessionMemberChanged;

        RequestedGameMode = inGameMode;
        ConnectionHandler.Initialization();
        
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> gameSessionConfig = GameSessionConfig.SessionCreateRequest;

        if (!gameSessionConfig.TryGetValue(inGameMode, out var matchTypeDict))
        {
            BytewarsLogger.LogWarning($"GameSession Configuration not found");
            return;
        }

        if (!matchTypeDict.TryGetValue(sessionServerType, out var request))
        {
            BytewarsLogger.LogWarning($"Matchtype not found");
            return;
        }

        request.attributes = CreateMatchConfig.CreatedMatchSessionAttribute;

        if (ConnectionHandler.IsUsingLocalDS())
        {
            request.serverName = ConnectionHandler.LocalServerName;
        }

        // Playing with Party additional code
        if (!String.IsNullOrWhiteSpace(PartyHelper.CurrentPartyId))
        {
            string[] memberIds = PartyHelper.PartyMembersData.Select(data => data.UserId).ToArray();
            
            request.teams = new SessionV2TeamData[]
            {
                new SessionV2TeamData()
                {
                    userIds = memberIds
                }
            };
        }

        CreateSession(request);
    }

    public void CancelCreateMatchSession()
    {
        LeaveCurrentGameSession();
    }

    public void CancelJoinMatchSession()
    {
        LeaveCurrentGameSession();
    }

    protected internal void BrowseMatchSession(Dictionary<string, object> request)
    {
        QueryGameSession(request);
    }

    protected internal void JoinMatchSession(string sessionId, InGameMode gameMode)
    {
        GameManager.Instance.OnClientLeaveSession += LeaveCurrentGameSession;
        lobby.SessionV2GameSessionMemberChanged += OnV2GameSessionMemberChanged;
        RequestedGameMode = gameMode;

        JoinSession(sessionId);
    }

    protected internal void LeaveCurrentGameSession()
    {
        GameManager.Instance.OnClientLeaveSession -= LeaveCurrentGameSession;
        lobby.SessionV2GameSessionMemberChanged -= OnV2GameSessionMemberChanged;
        LeaveSession(gameSession.id);
    }

    #region Event 

    protected internal void BindEvents()
    {
        OnCreateSessionCompleteEvent += OnCreateSessionCompleted;
        OnJoinSessionCompleteEvent += OnJoinCustomSessionCompleted;
        OnLeaveSessionCompleteEvent += OnLeaveCustomSessionCompleted;
        OnQueryGameSessionCompleteEvent += OnBrowseMatchSessionCompleted;
    }

    protected internal void UnbindEvents()
    {
        OnCreateSessionCompleteEvent -= OnCreateSessionCompleted;
        OnJoinSessionCompleteEvent -= OnJoinCustomSessionCompleted;
        OnLeaveSessionCompleteEvent -= OnLeaveCustomSessionCompleted;
        OnQueryGameSessionCompleteEvent -= OnBrowseMatchSessionCompleted;
    }

    #endregion


    #region Callback

    private void OnCreateSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            gameSession = result.Value;
            UpdateCachedGameSession(gameSession);
            BytewarsLogger.Log($"OnCreatedMatchSession: true");
            BytewarsLogger.Log($"Session Configuration Template Type : {gameSession.configuration.type}");
            OnCreatedMatchSession?.Invoke(true);
            switch (gameSession.configuration.type)
            {
                case SessionConfigurationTemplateType.DS:
                    MatchSessionDSWrapper.OnCreateMatchSessionDS?.Invoke();
                    break;
                case SessionConfigurationTemplateType.P2P:
                    MatchSessionP2PWrapper.OnCreateMatchSessionP2P?.Invoke(RequestedGameMode, result);
                    break;
                default:
                    break;
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            OnCreatedMatchSession?.Invoke(false);
        }
    }

    private void OnV2GameSessionMemberChanged(Result<SessionV2GameMembersChangedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2GameSession gameSession = result.Value.session;
            SessionCache.SetSessionLeaderId(gameSession.id, gameSession.leaderId);
            MatchSessionHelper.RefreshLobbyMenu();
        } 
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }
    }

    private void OnJoinCustomSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError) 
        {
            gameSession = result.Value;
            UpdateCachedGameSession(gameSession);
            BytewarsLogger.Log($"Session Configuration Template Type : {gameSession.configuration.type}");
            switch (gameSession.configuration.type)
            {
                case SessionConfigurationTemplateType.DS:
                    MatchSessionDSWrapper.OnJoinMatchSessionDS.Invoke(result);
                    break;
                case SessionConfigurationTemplateType.P2P:
                    MatchSessionP2PWrapper.OnJoinMatchSessionP2P?.Invoke(RequestedGameMode ,result);
                    break;
                default:
                    break;
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
            OnCreateOrJoinError?.Invoke(result.Error.Message);
        }
    }

    private void OnLeaveCustomSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success leave session id: {gameSession?.id ?? "sessionId"}");
            OnLeaveSessionCompleted?.Invoke();
        }
        else
        {
            BytewarsLogger.LogWarning($"Error leave session: {result.Error.Message}");
        }
    }

    private void OnBrowseMatchSessionCompleted(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        OnBrowseMatchSessionCompleteEvent?.Invoke(result);
    }

    #endregion

    private void UpdateCachedGameSession(SessionV2GameSession session)
    {
        gameSession = session;
        SessionCache.SetJoinedSessionIdAndLeaderUserId(session.id, session.leaderId);
    }
}
