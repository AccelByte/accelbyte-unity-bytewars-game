// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class BrowseMatchSessionWrapper : MatchSessionWrapper
{
    private static string nextPage;
    private static bool isBrowseMatchSessionsCanceled;
    private static bool isJoinMatchSessionCancelled;
    private static bool isQueryingNextMatchSessions;
    private static Action<BrowseMatchResult> onQueryMatchSessionFinished;
    private static Action<BrowseMatchResult> onQueryNextPageMatchSessionFinished;

    protected internal void BindEvents()
    {
        base.BindEvents();
        OnBrowseMatchSessionCompleteEvent += OnBrowseMatchSessionsComplete;
    }

    protected internal void UnbindEvents()
    {
        base.UnbindEvents();
        OnBrowseMatchSessionCompleteEvent -= OnBrowseMatchSessionsComplete;
    }

    #region BrowseMatchSession

    protected internal void BrowseMatch(Action<BrowseMatchResult> onSessionRetrieved)
    {
        nextPage = string.Empty;
        isBrowseMatchSessionsCanceled = false;
        onQueryMatchSessionFinished = onSessionRetrieved;
        BrowseMatchSession(CreateMatchConfig.CreatedMatchSessionAttribute);
    }

    public void CancelBrowseMatchSessions()
    {
        isBrowseMatchSessionsCanceled = true;
    }

    #endregion

    #region QueryMatchSession

    public void QueryNextMatchSessions(Action<BrowseMatchResult> onQueryNextMatchSessionsFinished)
    {
        if (String.IsNullOrEmpty(nextPage))
        {
            onQueryNextMatchSessionsFinished?
                .Invoke(new BrowseMatchResult(Array.Empty<SessionV2GameSession>()));
            isQueryingNextMatchSessions = false;
        }
        else
        {
            if (!isQueryingNextMatchSessions)
            {
                var req = GenerateRequestFromNextPage(nextPage);
                onQueryNextPageMatchSessionFinished = onQueryNextMatchSessionsFinished;
                session?.QueryGameSession(req, OnQueryNextPageFinished);
            }
        }
    }

    private void OnQueryNextPageFinished(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        if (result.IsError)
        {
            onQueryNextPageMatchSessionFinished?.Invoke(new BrowseMatchResult(null, result.Error.Message));
        }
        else
        {
            nextPage = result.Value.paging.next;
            if (!String.IsNullOrEmpty(nextPage))
                BytewarsLogger.Log($"next page: {nextPage}");
            onQueryNextPageMatchSessionFinished?.Invoke(new BrowseMatchResult(result.Value.data));
        }
        isQueryingNextMatchSessions = false;
    }

    private static Dictionary<string, object> GenerateRequestFromNextPage(string nextPageUrl)
    {
        isQueryingNextMatchSessions = true;
        var result = CreateMatchConfig.CreatedMatchSessionAttribute;
        var fullUrl = nextPageUrl.Split('?');
        if (fullUrl.Length < 2)
            return result;
        var joinedParameters = fullUrl[1];
        var parameters = joinedParameters.Split('&');
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var keyValue = parameter.Split('=');
            if (keyValue.Length < 2)
                return result;
            result.Add(keyValue[0], keyValue[1]);
        }
        return result;
    }

    #endregion

    #region EventHandler

    private void OnBrowseMatchSessionsComplete(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        if (!result.IsError)
        {
            if (!isBrowseMatchSessionsCanceled)
                onQueryMatchSessionFinished?.Invoke(new BrowseMatchResult(result.Value.data));
            nextPage = result.Value.paging.next;
        }
        else
        {
            if (!isBrowseMatchSessionsCanceled)
                onQueryMatchSessionFinished?.Invoke(new BrowseMatchResult(null, result.Error.Message));
        }
    }

    #endregion

    #region GetUserDisplayname

    public static void GetUserDisplayName(string userId, ResultCallback<PublicUserData> onPublicUserDataRetrieved)
    {
        AccelByteSDK.GetClientRegistry().GetApi().GetUser()
            .GetUserByUserId(userId, onPublicUserDataRetrieved);
    }

    #endregion
}