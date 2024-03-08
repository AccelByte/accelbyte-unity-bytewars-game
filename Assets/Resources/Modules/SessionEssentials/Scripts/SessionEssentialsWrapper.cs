// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Runtime.CompilerServices;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SessionEssentialsWrapper : MonoBehaviour
{
    protected Session Session;
    protected Lobby Lobby;

    /// <summary>
    /// This event will raised after JoinSession is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnJoinSessionCompleteEvent;

    /// <summary>
    /// This event will raised after CreateSession is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnCreateSessionCompleteEvent;

    /// <summary>
    /// This event will raised after LeaveSession is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnLeaveSessionCompleteEvent;

    /// <summary>
    /// This event will raised after GetGameSessionDetailsById is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnGetSessionDetailsCompleteEvent;

    protected void Awake()
    {
        Session = MultiRegistry.GetApiClient().GetSession();
        Lobby = MultiRegistry.GetApiClient().GetLobby();
    }

    private void Start()
    {
        LoginHandler.onLoginCompleted += LoginToLobby;
    }

    /// <summary>
    /// This method will invoke OnCreateSessionCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionRequest"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected internal void CreateSession(SessionRequestPayload sessionRequest, [CallerFilePath] string? sourceFilePath = null)
    {
        var gameSessionRequest = SessionUtil.CreateGameSessionRequest(sessionRequest);
        var tutorialType = SessionUtil.GetTutorialTypeFromClass(sourceFilePath);
        Session.CreateGameSession(gameSessionRequest, result => OnCreateSessionCompleted(result, tutorialType));
    }

    /// <summary>
    /// This method will invoke OnJoinSessionCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected internal void JoinSession(string sessionId, [CallerFilePath] string? sourceFilePath = null)
    {
        var tutorialType = SessionUtil.GetTutorialTypeFromClass((sourceFilePath));
        BytewarsLogger.Log($" sessionId {sessionId} ");
        Session.JoinGameSession(sessionId, result => OnJoinSessionCompleted(result, tutorialType));
    }

    protected internal void JoinSession(string sessionId, TutorialType tutorialType)
    {
        BytewarsLogger.Log($" sessionId {sessionId} ");
        Session.JoinGameSession(sessionId, result => OnJoinSessionCompleted(result, tutorialType));
    }

    /// <summary>
    /// This method will invoke OnLeaveSessionCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected internal void LeaveSession(string sessionId, [CallerFilePath] string? sourceFilePath = null)
    {
        var tutorialType = SessionUtil.GetTutorialTypeFromClass(sourceFilePath);
        Session.LeaveGameSession(sessionId, result => OnLeaveSessionCompleted(result, tutorialType));
    }

    protected internal void LeaveSession(string sessionId, TutorialType tutorialType)
    {
        Session.LeaveGameSession(sessionId, result => OnLeaveSessionCompleted(result, tutorialType));
    }

    /// <summary>
    /// This method will invoke OnGetSessionDetailsCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected void GetGameSessionDetailsById(string sessionId, [CallerFilePath] string? sourceFilePath = null)
    {
        var tutorialType = SessionUtil.GetTutorialTypeFromClass(sourceFilePath);
        Session.GetGameSessionDetailsBySessionId(sessionId, result => OnGetGameSessionDetailsByIdComplete(result, tutorialType));
    }

    private void LoginToLobby(TokenData tokenData)
    {
        if (!Lobby.IsConnected)
        {
            Lobby.Connect();
        }
    }

    #region Callback
    /// <summary>
    /// CreateSession Callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnCreateSessionCompleted(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();

        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully created game session");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            response.IsError = result.IsError;
        }
        OnCreateSessionCompleteEvent?.Invoke(response);
    }

    /// <summary>
    /// JoinSession Callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnJoinSessionCompleted(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();

        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully joined the game session");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            response.IsError = result.IsError;
        }
        OnJoinSessionCompleteEvent?.Invoke(response);
    }

    /// <summary>
    /// LeaveSession callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnLeaveSessionCompleted(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();

        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully left the game session");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            response.IsError = result.IsError;
        }
        OnLeaveSessionCompleteEvent?.Invoke(response);
    }

    /// <summary>
    /// GetGameSessionDetailsById callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnGetGameSessionDetailsByIdComplete(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();

        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully obtained the game session details");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error}");
            response.IsError = result.IsError;
        }
        OnGetSessionDetailsCompleteEvent?.Invoke(response);
    }
    #endregion Callback
}
