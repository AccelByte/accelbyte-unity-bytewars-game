using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AccelByte.Core;
using AccelByte.Models;

public class MatchSessionWrapper : GameSessionEssentialsWrapper
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
    protected void CreateCustomMatchSession(SessionV2GameSessionCreateRequest request)
    {
        Session.CreateGameSession(request, OnCreateCustomMatchSessionComplete);
    }

    /// <summary>
    /// Function to Browse Custom Match Session
    /// </summary>
    /// <param name="request"></param>
    protected void BrowseCustomMatchSession(Dictionary<string, object> request = null)
    {
        Session.QueryGameSession(request, OnBrowseMatchSessionComplete);
    }

    /// <summary>
    /// Function to Join Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    protected void JoinCustomMatchSession(string sessionId)
    {
        Session.JoinGameSession(sessionId, OnJoinCustomSessionComplete);
    }

    /// <summary>
    /// Function to Leave Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    protected void LeaveCustomMatchSession(string sessionId)
    {
        Session.LeaveGameSession(sessionId, OnLeaveCustomSessionComplete);
    }

    /// <summary>
    /// Function to Delete Custom Match Session
    /// </summary>
    /// <param name="sessionId"></param>
    protected internal void DeleteCustomMatchSession(string sessionId)
    {
        Session.DeleteGameSession(sessionId, OnDeleteCustomSessionComplete);
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
