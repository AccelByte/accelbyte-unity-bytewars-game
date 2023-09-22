using System;
using System.Collections;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionDSWrapper_Starter : MatchSessionWrapper
{
    private static bool _isJoinMatchSessionCancelled;
    private static Action<string> _onJoinedMatchSession;

    private static bool _isCreateMatchSessionCancelled;
    private static Action<string> _onCreatedMatchSession;
    
    public static event Action<SessionV2GameSession> OnGameSessionUpdated;
    private static MatchSessionServerType _requestedSessionServerType = MatchSessionServerType.DedicatedServer;
    private static InGameMode _requestedGameMode = InGameMode.None;
    private static SessionV2GameSession _v2GameSession;
    
    private static MatchSessionWrapper _instance;
    private static readonly WaitForSeconds _waitOneSec = new WaitForSeconds(1);
    private const int MaxCheckDSStatusCount = 10;
    private static int _checkDSStatusCount = 0;


    private void Awake()
    {
        base.Awake();
        _instance = this;
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
        _isCreateMatchSessionCancelled = false;
        _requestedSessionServerType = sessionServerType;
        _requestedGameMode = gameMode;
        _v2GameSession = null;
        var config = MatchSessionConfig.MatchRequests;
        if (!config.TryGetValue(gameMode, out var matchTypeDict)) return;
        if (!matchTypeDict.TryGetValue(sessionServerType, out var request)) return;
        BytewarsLogger.Log($"creating session {gameMode} {sessionServerType}");
        _onCreatedMatchSession = onCreatedMatchSession;
        var isUsingLocalDS = ConnectionHandler.GetArgument();
        if (isUsingLocalDS)
        {
            request.serverName = ConnectionHandler.LocalServerName;
        }
        CreateCustomMatchSession(request);
    }
    private void OnCreateGameSessionResult(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.Log($"error: {result.Error.Message}");
            _onCreatedMatchSession?.Invoke(result.Error.Message);
        }
        else
        {
            BytewarsLogger.Log($"create session result: {result.Value.ToJsonString()}");
            if (_isCreateMatchSessionCancelled) return;
            _v2GameSession = result.Value;
            SessionCache.SetJoinedSessionIdAndLeaderUserId(_v2GameSession.id, _v2GameSession.leaderId);
            _checkDSStatusCount = 0;
            _instance.StartCoroutine(CheckSessionDetails());
        }
    }
    
    private IEnumerator CheckSessionDetails()
    {
        if(_v2GameSession==null)
        {
            _onCreatedMatchSession?.Invoke("Error Unable to create session");
            yield break;
        }
        yield return _waitOneSec;
        _checkDSStatusCount++;
        _session.GetGameSessionDetailsBySessionId(_v2GameSession.id, OnSessionDetailsCheckFinished);
    }
    private void OnSessionDetailsCheckFinished(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"{result.ToJsonString()}");
        if(_isCreateMatchSessionCancelled)return;
        if(result.IsError)
        {
            string errorMessage = result.Error.Message;
            _onCreatedMatchSession?.Invoke(errorMessage);
        }
        else
        {
            if(result.Value.dsInformation.status==SessionV2DsStatus.AVAILABLE)
            {
                _onCreatedMatchSession?.Invoke("");
                _onCreatedMatchSession = null;
                TravelToDS(result.Value, _requestedGameMode);
            }
            else
            {
                if(_checkDSStatusCount>=MaxCheckDSStatusCount)
                {
                    _onCreatedMatchSession?.Invoke("Failed to Connect to Dedicated Server in time");
                }
                else
                {
                    _instance.StartCoroutine(CheckSessionDetails());
                }
            }
        }
    }

    
    public void CancelCreateMatchSession()
    {
        _isCreateMatchSessionCancelled = true;
        LeaveGameSession();
    }

    #endregion

    #region JoinCustomMatchSession

    public void JoinMatchSession(string sessionId, 
        InGameMode gameMode,
        Action<string> onJoinedGameSession)
    {
        _isJoinMatchSessionCancelled = false;
        _onJoinedMatchSession = onJoinedGameSession;
        _requestedGameMode = gameMode;
        JoinCustomMatchSession(sessionId);
    }
    
    private void OnJoinMatchSessionComplete(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            if(!_isJoinMatchSessionCancelled)
                _onJoinedMatchSession?.Invoke(result.Error.Message);
        }
        else
        {
            var gameSession = result.Value;
            if (gameSession.configuration.type == SessionConfigurationTemplateType.DS)
            {
                if (gameSession.dsInformation.status == SessionV2DsStatus.AVAILABLE)
                {
                    if (_isJoinMatchSessionCancelled) return;
                    SetJoinedGameSession(gameSession);
                    SessionCache.SetJoinedSessionIdAndLeaderUserId(gameSession.id, gameSession.leaderId);
                    TravelToDS(gameSession, _requestedGameMode);
                }
                else
                {
                    LeaveSessionWhenFailed(gameSession.id);
                    if(!_isJoinMatchSessionCancelled)
                        _onJoinedMatchSession?.Invoke("Failed to join _session, no response from the server");
                }
            }
        }
    }
    
    public void CancelJoinMatchSession()
    {
        _isJoinMatchSessionCancelled = true;
    }

    #endregion

    #region LeaveCustomSession

    private void LeaveGameSession()
    {
        if (_v2GameSession == null) return;
        LeaveCustomMatchSession(_v2GameSession.id);
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
            if (_v2GameSession == null ||
                !_v2GameSession.id.Equals(result.Value.sessionId))
            {
                BytewarsLogger.LogWarning($"unmatched DS session id is received");
                return;
            }
            // if (_isCreateMatchSessionCancelled) return;
            _v2GameSession = result.Value.session;
            // OnGameSessionUpdated?.Invoke(_v2GameSession);
            if (_isCreateMatchSessionCancelled) return;
            if (_v2GameSession.dsInformation.status != SessionV2DsStatus.AVAILABLE) return;
            OnGameSessionUpdated?.Invoke(_v2GameSession);
            TravelToDS(_v2GameSession, _requestedGameMode);
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
        // BytewarsLogger.LogResult(result);
    }
    
    private void OnLoginSuccess(TokenData tokenData)
    {
        MatchSessionHelper.GetCurrentUserPublicData(tokenData.user_id);
        if(!_lobby.IsConnected)
            _lobby.Connect();
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
            BytewarsLogger.Log($"success leave session id: {_v2GameSession.id}");
        }

        if (_isCreateMatchSessionCancelled)
        {
            DeleteCustomMatch(_v2GameSession.id);
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
        _v2GameSession = gameSession;
    }

    #endregion

    #region Debug
    public void GetDetail()
    {
        if (_v2GameSession == null) return;
        _session.GetGameSessionDetailsBySessionId(_v2GameSession.id, OnSessionDetailsRetrieved);
    }
    private void OnSessionDetailsRetrieved(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"OnSessionDetailsRetrieved currentUserId:{MultiRegistry.GetApiClient().session.UserId}");
        MatchSessionHelper.LogResult(result);
    }
    #endregion Debug
}
