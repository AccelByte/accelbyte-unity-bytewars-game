// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;

public class MatchSessionWrapper : GameSessionUtilityWrapper
{
    public Action OnCreateMatchCancelled;
    protected internal event Action<Result<PaginatedResponse<SessionV2GameSession>>> OnBrowseMatchSessionCompleteEvent;
    protected internal event Action OnLeaveSessionCompleted;
    protected internal Action<bool> OnCreatedMatchSession;
    protected internal Action<string> OnJoinedMatchSession;
    protected internal Action<string> OnCreateOrJoinError;
    protected internal InGameMode SelectedGameMode = InGameMode.None;
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
        GameManager.OnDisconnectedInMainMenu += DisconnectFromServer;
        lobby.SessionV2GameSessionMemberChanged += OnV2GameSessionMemberChanged;

        SelectedGameMode = inGameMode;
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

        // Add preferred regions.
        string[] preferredRegions = RegionPreferencesHelper.GetEnabledRegions().Select(x => x.RegionCode).ToArray();
        if (preferredRegions.Length > 0)
        {
            request.requestedRegions = preferredRegions;
        }

        // Playing with Party additional code
        if (!string.IsNullOrEmpty(PartyHelper.CurrentPartyId))
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
        GameManager.OnDisconnectedInMainMenu += DisconnectFromServer;
        lobby.SessionV2GameSessionMemberChanged += OnV2GameSessionMemberChanged;
        SelectedGameMode = gameMode;

        JoinSession(sessionId);
    }

    private void DisconnectFromServer(string reason)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            OnCreateOrJoinError?.Invoke(reason);
            BytewarsLogger.LogWarning(reason);
        } 
        else
        {
            LeaveCurrentGameSession();
        }
    }

    protected internal void LeaveCurrentGameSession()
    {
        GameManager.Instance.OnClientLeaveSession -= LeaveCurrentGameSession;
        GameManager.OnDisconnectedInMainMenu -= DisconnectFromServer;
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
            BytewarsLogger.Log($"Successfully created the match session");  
            BytewarsLogger.Log($"OnCreatedMatchSession: true");
            BytewarsLogger.Log($"Session Configuration Template Type : {gameSession.configuration.type}");
            OnCreatedMatchSession?.Invoke(true);
            bool isStarterActive = false;
            switch (gameSession.configuration.type)
            {
                case SessionConfigurationTemplateType.DS:
                    isStarterActive = IsStarterActive(TutorialType.MatchmakingWithDS);
                    HandleCreateMatchSessionDS(isStarterActive, result);
                    break;
                case SessionConfigurationTemplateType.P2P:
                    isStarterActive = IsStarterActive(TutorialType.MatchmakingWithP2P);
                    HandleCreateMatchSessionP2P(isStarterActive, SelectedGameMode, result);
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
            BytewarsLogger.Log($"Successfully joined the match session");
            BytewarsLogger.Log($"Session Configuration Template Type : {gameSession.configuration.type}");
            bool isStarterActive = false;
            switch (gameSession.configuration.type)
            {
                case SessionConfigurationTemplateType.DS:
                    isStarterActive = IsStarterActive(TutorialType.MatchSessionWithDS);
                    HandleJoinsMatchSessionDS(isStarterActive, result);
                    break;
                case SessionConfigurationTemplateType.P2P:
                    isStarterActive = IsStarterActive(TutorialType.MatchmakingWithP2P);
                    HandleJoinsMatchSessionP2P(isStarterActive, SelectedGameMode, result);
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

    private void HandleCreateMatchSessionDS(bool isStarterActive, Result<SessionV2GameSession> result)
    {
        if (isStarterActive)
        {
            MatchSessionDSWrapper_Starter.OnCreateMatchSessionDS.Invoke();
        }
        else
        {
            MatchSessionDSWrapper.OnCreateMatchSessionDS?.Invoke();
        }
    }

    private void HandleJoinsMatchSessionDS(bool isStarterActive, Result<SessionV2GameSession> result)
    {
        if (isStarterActive)
        {
            MatchSessionDSWrapper_Starter.OnJoinMatchSessionDS.Invoke(SelectedGameMode, result);
        }
        else
        {
            MatchSessionDSWrapper.OnJoinMatchSessionDS.Invoke(SelectedGameMode, result);
        }
    }

    private void HandleCreateMatchSessionP2P(bool isStarterActive, InGameMode selectedGameMode, Result<SessionV2GameSession> result)
    {
        if (isStarterActive)
        {
            MatchSessionP2PWrapper_Starter.OnCreateMatchSessionP2P.Invoke(SelectedGameMode, result);
        }
        else
        {
            MatchSessionP2PWrapper.OnCreateMatchSessionP2P?.Invoke(SelectedGameMode, result);
        }
    }

    private void HandleJoinsMatchSessionP2P(bool isStarterActive, InGameMode selectedGameMode, Result<SessionV2GameSession> result)
    {
        if (isStarterActive)
        {
            MatchSessionP2PWrapper_Starter.OnJoinMatchSessionP2P?.Invoke(selectedGameMode, result);
        }
        else
        {
            MatchSessionP2PWrapper.OnJoinMatchSessionP2P?.Invoke(selectedGameMode, result);
        }
    }


    private void OnLeaveCustomSessionCompleted(Result result)
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

    private static bool IsStarterActive(TutorialType tutorialType)
    {
        ModuleModel module = TutorialModuleManager.Instance.GetModule(tutorialType);

        return module.isStarterActive;
    }
}
