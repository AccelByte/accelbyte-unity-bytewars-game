// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionP2PWrapper : MatchSessionWrapper
{
    private static bool isCreateMatchSessionCancelled;
    private static Action<string> onCreatedMatchSession;
    private static Action<bool> onEnableCancelButton;
    public static event Action<SessionV2GameSession> OnGameSessionUpdated;
    private static InGameMode requestedGameMode = InGameMode.None;
    private static SessionV2GameSession gameSessionV2;
    private static MatchSessionWrapper matchSessionWrapper;
    private static readonly WaitForSeconds waitOneSec = new WaitForSeconds(3);

    private void Awake()
    {
        base.Awake();
        matchSessionWrapper = this;
    }

    // Start is called before the first frame update
    void Start()
    {

        GameManager.Instance.OnClientLeaveSession += LeaveGameSession;
        LoginHandler.onLoginCompleted += OnLoginSuccess;

        OnCreateCustomMatchSessionCompleteEvent += OnCreateGameSessionResult;
        OnLeaveCustomSessionCompleteEvent += OnLeaveGameSession;
    }

    #region CreateCustomMatchSession
    public void CreateP2P(InGameMode gameMode,
        GameSessionServerType sessionServerType,
        Action<string> onCreatedMatchSession)
    {
        isCreateMatchSessionCancelled = false;
        requestedGameMode = gameMode;
        gameSessionV2 = null;
        var config = GameSessionConfig.SessionCreateRequest;
        if (!config.TryGetValue(gameMode, out var matchTypeDict))
        {
            return;
        }
        if (!matchTypeDict.TryGetValue(sessionServerType, out var request))
        {
            return;
        }
        BytewarsLogger.Log($"creating session {gameMode} {sessionServerType}");
        MatchSessionP2PWrapper.onCreatedMatchSession = onCreatedMatchSession;
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
            // BytewarsLogger.Log($"create session result: {result.Value.ToJsonString()}");
            onEnableCancelButton?.Invoke(false);
            if (isCreateMatchSessionCancelled)
            {
                onCreatedMatchSession?.Invoke("Match session creation cancelled");
                return;
            }
            gameSessionV2 = result.Value;
            SessionCache
                .SetJoinedSessionIdAndLeaderUserId(gameSessionV2.id, gameSessionV2.leaderId);
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

        if (isCreateMatchSessionCancelled)
        {
            onCreatedMatchSession?.Invoke("Match session creation cancelled");
            yield break;
        }

        session.GetGameSessionDetailsBySessionId(gameSessionV2.id, OnSessionDetailsCheckFinished);
    }

    private void OnSessionDetailsCheckFinished(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            string errorMessage = result.Error.Message;
            onCreatedMatchSession?.Invoke(errorMessage);
        }
        else
        {
            if (isCreateMatchSessionCancelled)
            {
                onCreatedMatchSession?.Invoke("Match session creation cancelled");
                return;
            }

            if (result.Value.configuration.type == SessionConfigurationTemplateType.P2P)
            {
                GameData.ServerSessionID = result.Value.id;
                P2PHelper.StartAsHost(requestedGameMode, result.Value.id);
            }
        }
    }

    public void CancelCreateMatchSessionP2P()
    {
        isCreateMatchSessionCancelled = true;
        BytewarsLogger.Log($"{isCreateMatchSessionCancelled}");
        LeaveGameSession();
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
    /// Leave game session if failed to connect to game server
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
}