using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class BrowseMatchSessionWrapper_Starter : MatchSessionWrapper
{
    private static string nextPage;
    private static bool isBrowseMatchSessionsCanceled;
    private static bool isJoinMatchSessionCancelled;
    private static bool isQueryingNextMatchSessions;
    private static Action<BrowseMatchResult> onQueryMatchSessionFinished;
    private static Action<BrowseMatchResult> onQueryNextPageMatchSessionFinished;

    private void Start()
    {
        // Copy Start code here
    }

    #region BrowseMatchSession

    protected internal void BrowseMatch(Action<BrowseMatchResult> onSessionRetrieved)
    {
        // Copy BrowseMatch code here
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
                Session?.QueryGameSession(req, OnQueryNextPageFinished);
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
        var result = MatchSessionConfig.CreatedMatchSessionAttribute;
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
        // Copy OnBrowseMatchSessionsComplete here
    }

    #endregion

    #region GetUserDisplayname

    public static void GetUserDisplayName(string userId, ResultCallback<PublicUserData> onPublicUserDataRetrieved)
    {
        MultiRegistry.GetApiClient().GetUser()
            .GetUserByUserId(userId, onPublicUserDataRetrieved);
    }

    #endregion


}