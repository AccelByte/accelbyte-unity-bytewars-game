// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SessionEssentialsWrapper : MonoBehaviour
{
    protected Session session;
    protected Lobby lobby;

    /// <summary>
    /// This event will raised after JoinSession is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnJoinSessionCompleteEvent;

    /// <summary>
    /// This event will raised after CreateSession is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnCreateSessionCompleteEvent;

    /// <summary>
    /// This event will raised after LeaveSession is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnLeaveSessionCompleteEvent;

    /// <summary>
    /// This event will raised after GetGameSessionDetailsById is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnGetSessionDetailsCompleteEvent;

    protected void Awake()
    {
        session = AccelByteSDK.GetClientRegistry().GetApi().GetSession();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
    }


    /// <summary>
    /// This method will invoke OnCreateSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionRequest"></param>
    protected internal void CreateSession(SessionV2GameSessionCreateRequest request)
    {
        session.CreateGameSession(request, OnCreateSessionCompleted);
    }

    /// <summary>
    /// This method will invoke OnJoinSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void JoinSession(string sessionId)
    {
        session.JoinGameSession(sessionId, OnJoinSessionCompleted);
    }

    /// <summary>
    /// This method will invoke OnLeaveSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void LeaveSession(string sessionId)
    {
        session.LeaveGameSession(sessionId,  OnLeaveSessionCompleted);
    }

    /// <summary>
    /// This method will invoke OnGetSessionDetailsCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected void GetGameSessionDetailsById(string sessionId)
    {
        session.GetGameSessionDetailsBySessionId(sessionId, OnGetGameSessionDetailsByIdComplete);
    }

    #region Session Callback
    /// <summary>
    /// CreateSession Callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnCreateSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully created game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }

        OnCreateSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// JoinSession Callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnJoinSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully joined the game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }

        OnJoinSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// LeaveSession callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnLeaveSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully left the game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }

        OnLeaveSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// GetGameSessionDetailsById callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnGetGameSessionDetailsByIdComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully obtained the game session details");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error}");
        }

        OnGetSessionDetailsCompleteEvent?.Invoke(result);
    }

    #endregion
}
