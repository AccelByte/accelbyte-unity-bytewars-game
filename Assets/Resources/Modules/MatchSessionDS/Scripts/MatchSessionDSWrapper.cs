// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionDSWrapper : MatchSessionWrapper
{
    private static bool isJoinMatchSessionCancelled;
    private static Action<string> onJoinedMatchSession;
    private static bool isCreateMatchSessionCancelled;
    private static Action<string> onCreatedMatchSession;
    public static event Action<SessionV2GameSession> OnGameSessionUpdated;
    private static MatchSessionServerType requestedSessionServerType =
        MatchSessionServerType.DedicatedServer;
    private static InGameMode requestedGameMode = InGameMode.None;
    private static SessionV2GameSession gameSessionV2;
    private static MatchSessionWrapper matchSessionWrapper;
    private static readonly WaitForSeconds waitOneSec = new WaitForSeconds(1);
    private const int MaxCheckDSStatusCount = 10;
    private static int checkDSStatusCount = 0;

    private void Awake()
    {
        base.Awake();
        matchSessionWrapper = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _lobby.SessionV2GameSessionMemberChanged += OnV2GameSessionMemberChanged;
        GameManager.Instance.OnClientLeaveSession += LeaveGameSession;
        LoginHandler.onLoginCompleted += OnLoginSuccess;

        OnJoinCustomSessionCompleteEvent += OnJoinMatchSessionComplete;
        OnCreateCustomMatchSessionCompleteEvent += OnCreateGameSessionResult;
        OnLeaveCustomSessionCompleteEvent += OnLeaveGameSession;
    }

    #region CreateCustomMatchSession
    public void Create(InGameMode gameMode,
        MatchSessionServerType sessionServerType,
        Action<string> onCreatedMatchSession)
    {
        isCreateMatchSessionCancelled = false;
        requestedSessionServerType = sessionServerType;
        requestedGameMode = gameMode;
        gameSessionV2 = null;
        var config = MatchSessionConfig.MatchRequests;
        if (!config.TryGetValue(gameMode, out var matchTypeDict))
        {
            return;
        }
        if (!matchTypeDict.TryGetValue(sessionServerType, out var request))
        {
            return;
        }
        BytewarsLogger.Log($"creating session {gameMode} {sessionServerType}");
        MatchSessionDSWrapper.onCreatedMatchSession = onCreatedMatchSession;
        ConnectionHandler.Initialization();
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

        CreateCustomMatchSession(request);
    }

    private void OnCreateGameSessionResult(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.Log($"error: {result.Error.Message}");
            onCreatedMatchSession?.Invoke(result.Error.Message);
        }
        else
        {
            BytewarsLogger.Log($"create session result: {result.Value.ToJsonString()}");
            if (isCreateMatchSessionCancelled)
            {
                return;
            }
            gameSessionV2 = result.Value;
            SessionCache
                .SetJoinedSessionIdAndLeaderUserId(gameSessionV2.id, gameSessionV2.leaderId);
            checkDSStatusCount = 0;
            matchSessionWrapper.StartCoroutine(CheckSessionDetails());
        }
    }

    private IEnumerator CheckSessionDetails()
    {
        if (gameSessionV2 == null)
        {
            onCreatedMatchSession?.Invoke("Error Unable to create session");
            yield break;
        }
        yield return waitOneSec;
        checkDSStatusCount++;
        _session.GetGameSessionDetailsBySessionId(gameSessionV2.id, OnSessionDetailsCheckFinished);
    }

    private void OnSessionDetailsCheckFinished(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"{result.ToJsonString()}");
        if (isCreateMatchSessionCancelled)
        {
            onCreatedMatchSession?.Invoke("Match session creation cancelled");
            return;
        }
        if (result.IsError)
        {
            string errorMessage = result.Error.Message;
            onCreatedMatchSession?.Invoke(errorMessage);
        }
        else
        {
            if (result.Value.dsInformation.status == SessionV2DsStatus.AVAILABLE)
            {
                onCreatedMatchSession?.Invoke("");
                onCreatedMatchSession = null;
                TravelToDS(result.Value, requestedGameMode);
            }
            else
            {
                if (checkDSStatusCount >= MaxCheckDSStatusCount)
                {
                    onCreatedMatchSession?.Invoke("Failed to Connect to Dedicated Server in time");
                }
                else
                {
                    matchSessionWrapper.StartCoroutine(CheckSessionDetails());
                }
            }
        }
    }

    public void CancelCreateMatchSession()
    {
        isCreateMatchSessionCancelled = true;
        LeaveGameSession();
    }
    #endregion

    #region JoinCustomMatchSession
    public void JoinMatchSession(string sessionId,
        InGameMode gameMode,
        Action<string> onJoinedGameSession)
    {
        isJoinMatchSessionCancelled = false;
        onJoinedMatchSession = onJoinedGameSession;
        requestedGameMode = gameMode;
        JoinCustomMatchSession(sessionId);
    }

    private void OnJoinMatchSessionComplete(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            if (!isJoinMatchSessionCancelled)
            {
                onJoinedMatchSession?.Invoke(result.Error.Message);
            }
        }
        else
        {
            var gameSession = result.Value;
            if (gameSession.configuration.type == SessionConfigurationTemplateType.DS)
            {
                if (gameSession.dsInformation.status == SessionV2DsStatus.AVAILABLE)
                {
                    if (isJoinMatchSessionCancelled)
                    {
                        return;
                    }
                    SetJoinedGameSession(gameSession);
                    SessionCache
                        .SetJoinedSessionIdAndLeaderUserId(gameSession.id, gameSession.leaderId);
                    TravelToDS(gameSession, requestedGameMode);
                }
                else
                {
                    LeaveSessionWhenFailed(gameSession.id);
                    if (!isJoinMatchSessionCancelled)
                    {
                        onJoinedMatchSession?
                            .Invoke("Failed to join _session, no response from the server");
                    }
                }
            }
        }
    }

    public void CancelJoinMatchSession()
    {
        isJoinMatchSessionCancelled = true;
    }
    #endregion

    #region LeaveCustomSession
    private void LeaveGameSession()
    {
        if (gameSessionV2 == null)
        {
            return;
        }
        LeaveCustomMatchSession(gameSessionV2.id);
    }

    /// <summary>
    /// leave game session if failed to connect to game server
    /// </summary>
    /// <param name="sessionId">session id to leave</param>
    private void LeaveSessionWhenFailed(string sessionId)
    {
        LeaveCustomMatchSession(sessionId);
    }
    #endregion

    #region DeleteCustomSession
    private void DeleteCustomMatch(string sessionId)
    {
        DeleteCustomMatchSession(sessionId);
    }
    #endregion

    #region Events
    private void OnV2DSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"receiving DS status change error: {result.Error.Message}");
        }
        else
        {
            if (gameSessionV2 == null ||
                !gameSessionV2.id.Equals(result.Value.sessionId))
            {
                BytewarsLogger.LogWarning($"unmatched DS session id is received");
                return;
            }
            gameSessionV2 = result.Value.session;
            if (isCreateMatchSessionCancelled ||
                gameSessionV2.dsInformation.status != SessionV2DsStatus.AVAILABLE)
            {
                BytewarsLogger.LogWarning($"status changed to: {gameSessionV2.dsInformation.status}"
                    + $"is create match session cancelled: {isCreateMatchSessionCancelled}");
                return;
            }
            OnGameSessionUpdated?.Invoke(gameSessionV2);
            TravelToDS(gameSessionV2, requestedGameMode);
        }
    }

    private void OnV2GameSessionMemberChanged(Result<SessionV2GameMembersChangedNotification> result)
    {
        if (!result.IsError)
        {
            var gameSession = result.Value.session;
            SessionCache.SetSessionLeaderId(gameSession.id, gameSession.leaderId);
            OnGameSessionUpdated?.Invoke(gameSession);
        }
    }

    private void OnLoginSuccess(TokenData tokenData)
    {
        MatchSessionHelper.GetCurrentUserPublicData(tokenData.user_id);
        if (!_lobby.IsConnected)
        {
            _lobby.Connect();
        }
    }
    #endregion

    #region EventHandler
    private void OnLeaveGameSession(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"error leave session: {result.Error.Message}");
        }
        else
        {
            SessionCache.SetJoinedSessionId("");
            BytewarsLogger.Log($"success leave session id: {gameSessionV2.id}");
        }

        if (isCreateMatchSessionCancelled)
        {
            DeleteCustomMatch(gameSessionV2.id);
        }
    }
    #endregion

    #region Utils
    /// <summary>
    /// cached joined game session
    /// </summary>
    /// <param name="gameSession">joined game session</param>
    private void SetJoinedGameSession(SessionV2GameSession gameSession)
    {
        gameSessionV2 = gameSession;
    }
    #endregion

    #region Debug
    public void GetDetail()
    {
        if (gameSessionV2 == null)
        {
            return;
        }
        _session.GetGameSessionDetailsBySessionId(gameSessionV2.id, OnSessionDetailsRetrieved);
    }

    private void OnSessionDetailsRetrieved(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"OnSessionDetailsRetrieved currentUserId:{MultiRegistry.GetApiClient().session.UserId}");
        MatchSessionHelper.LogResult(result);
    }
    #endregion Debug
}