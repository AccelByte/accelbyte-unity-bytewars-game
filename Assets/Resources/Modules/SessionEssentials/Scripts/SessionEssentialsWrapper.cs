// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SessionEssentialsWrapper : MonoBehaviour
{
    protected Session session;
    protected Lobby lobby;

    /// <summary>
    /// This event will be raised after JoinSession is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnJoinSessionCompleteEvent;

    /// <summary>
    /// This event will be raised after CreateSession is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnCreateSessionCompleteEvent;

    /// <summary>
    /// This event will be raised after LeaveSession is complete
    /// </summary>
    public event Action<Result> OnLeaveSessionCompleteEvent;

    /// <summary>
    /// This event will be raised after GetGameSessionDetailsById is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnGetSessionDetailsCompleteEvent;

    /// <summary>
    /// This event will be raised after QueryGameSession is complete
    /// </summary>
    public event Action<Result<PaginatedResponse<SessionV2GameSession>>> OnQueryGameSessionCompleteEvent;

    /// <summary>
    /// This event will be raised after QueryGameSession is complete
    /// </summary>
    public event Action<bool> OnRejectGameSessionCompleteEvent;

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
    /// This method will invoke OnGetSessionDetailsCompleteEvent return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected void GetGameSessionDetailsById(string sessionId)
    {
        session.GetGameSessionDetailsBySessionId(sessionId, OnGetGameSessionDetailsByIdComplete);
    }

    /// <summary>
    /// This method will invoke OnQueryGameSessionCompleteEvent return Result<PaginatedResponse<SessionV2GameSession>>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected void QueryGameSession(Dictionary<string, object> request)
    {
        session.QueryGameSession(request, OnQueryGameSessionCompleted);
    }

    /// <summary>
    /// This method will invoke OnJoinSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void RejectSession(string sessionId)
    {
        session.RejectGameSessionInvitation(sessionId, OnRejectGameSessionCompleted);
    }

    #region Session Callback
    /// <summary>
    /// CreateSession Callback
    /// </summary>
    /// <param name="result"></param>
    private void OnCreateSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully created game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnCreateSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// JoinSession Callback
    /// </summary>
    /// <param name="result"></param>
    private void OnJoinSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully joined the game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnJoinSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// LeaveSession callback
    /// </summary>
    /// <param name="result"></param>
    private void OnLeaveSessionCompleted(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully left the game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnLeaveSessionCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// GetGameSessionDetailsById callback
    /// </summary>
    /// <param name="result"></param>
    private void OnGetGameSessionDetailsByIdComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully obtained the game session details");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnGetSessionDetailsCompleteEvent?.Invoke(result);
    }

    /// <summary>
    /// QueryGameSession callback
    /// </summary>
    /// <param name="result"></param>
    private void OnQueryGameSessionCompleted(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully obtained the game session details");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnQueryGameSessionCompleteEvent?.Invoke(result);

    }

    private void OnRejectGameSessionCompleted(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully rejected the game session");
        }
        else
        {
            BytewarsLogger.LogWarning($"Error : {result.Error.Message}");
        }

        OnRejectGameSessionCompleteEvent?.Invoke(result.IsError);
    }


    #endregion
}
