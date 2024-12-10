// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SessionEssentialsWrapper_Starter : MonoBehaviour
{
    protected Session session;
    protected Lobby lobby;

    /// <summary>
    /// This event will be raised after JoinSession is complete
    /// </summary>
    //TODO: Copy OnJoinSessionCompleteEvent event here

    /// <summary>
    /// This event will be raised after CreateSession is complete
    /// </summary>
    //TODO: Copy OnCreateSessionCompleteEvent event here

    /// <summary>
    /// This event will be raised after LeaveSession is complete
    /// </summary>
    //TODO: Copy OnLeaveSessionCompleteEvent event here

    /// <summary>
    /// This event will be raised after GetGameSessionDetailsById is complete
    /// </summary>
    public event Action<Result<SessionV2GameSession>> OnGetSessionDetailsCompleteEvent;

    /// <summary>
    /// This event will be raised after QueryGameSession is complete
    /// </summary>
    public event Action<Result<PaginatedResponse<SessionV2GameSession>>> OnQueryGameSessionCompleteEvent;

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
        //TODO: Copy your code here
    }

    /// <summary>
    /// This method will invoke OnJoinSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void JoinSession(string sessionId)
    {
        //TODO: Copy your code here
    }

    /// <summary>
    /// This method will invoke OnLeaveSessionCompleteEvent and return Result<SessionV2GameSession>.
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void LeaveSession(string sessionId)
    {
        //TODO: Copy your code here
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

    #region Session Callback
    /// <summary>
    /// CreateSession Callback
    /// </summary>
    /// <param name="result"></param>
    //TODO: Copy OnCreateSessionCompleted callback here

    /// <summary>
    /// JoinSession Callback
    /// </summary>
    /// <param name="result"></param>
    //TODO: Copy OnJoinSessionCompleted callback here

    /// <summary>
    /// LeaveSession callback
    /// </summary>
    /// <param name="result"></param>
    //TODO: Copy OnLeaveSessionCompleted callback here

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
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }

        OnQueryGameSessionCompleteEvent?.Invoke(result);
    }

    #endregion
}
