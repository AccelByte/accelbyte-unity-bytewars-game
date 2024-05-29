// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionWrapper : GameSessionUtilityWrapper
{

    protected event Action<Result<PaginatedResponse<SessionV2GameSession>>> OnBrowseMatchSessionCompleteEvent;
    protected event Action<Result<SessionV2GameSession>> OnJoinCustomSessionCompleteEvent;
    protected event Action<Result<SessionV2GameSession>> OnCreateCustomMatchSessionCompleteEvent;
    protected event Action<Result<SessionV2GameSession>> OnLeaveCustomSessionCompleteEvent;
    protected event Action OnDeleteCustomSessionCompleteEvent;

    protected void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Function to Create Custom Match Session 
    /// </summary>
    /// <param name="request"></param>
    protected internal void CreateCustomMatchSession(SessionV2GameSessionCreateRequest request)
    {
        session.CreateGameSession(request, OnCreateCustomMatchSessionComplete);
    }

    /// <summary>
    /// Function to Browse Custom Match Session
    /// </summary>
    /// <param name="request"></param>
    protected internal void BrowseCustomMatchSession(Dictionary<string, object> request = null)
    {
        session.QueryGameSession(request, OnBrowseMatchSessionComplete);
    }

    /// <summary>
    /// Function to Join Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void JoinCustomMatchSession(string sessionId)
    {
        session.JoinGameSession(sessionId, OnJoinCustomSessionComplete);
    }

    /// <summary>
    /// Function to Leave Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    public void LeaveCustomMatchSession(string sessionId)
    {
        session.LeaveGameSession(sessionId, OnLeaveCustomSessionComplete);
    }

    /// <summary>
    /// Function to Delete Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void DeleteCustomMatchSession(string sessionId)
    {
        session.DeleteGameSession(sessionId, OnDeleteCustomSessionComplete);
    }

    #region Callback

    private void OnCreateCustomMatchSessionComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success to create custom match session");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
        OnCreateCustomMatchSessionCompleteEvent?.Invoke(result);
    }

    private void OnBrowseMatchSessionComplete(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success getting match sessions");
        }
        else
        {
            BytewarsLogger.LogWarning($"error:{result.Error.ToJsonString()}");
        }
        OnBrowseMatchSessionCompleteEvent?.Invoke(result);
    }

    private void OnJoinCustomSessionComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success to join custom match session");
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
        }
        OnJoinCustomSessionCompleteEvent?.Invoke(result);
    }

    private void OnLeaveCustomSessionComplete(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success to leave custom match sessions");
            SessionCache.CurrentGameSessionId = string.Empty;
        }
        else
        {
            BytewarsLogger.LogWarning($"error:{result.Error.ToJsonString()}");
        }

        OnLeaveCustomSessionCompleteEvent?.Invoke(result);
    }

    private void OnDeleteCustomSessionComplete(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Success to delete custom match sessions");
        }
        else
        {
            BytewarsLogger.LogWarning($"error:{result.Error.ToJsonString()}");
        }
        OnDeleteCustomSessionCompleteEvent?.Invoke();
    }

    #endregion

}
