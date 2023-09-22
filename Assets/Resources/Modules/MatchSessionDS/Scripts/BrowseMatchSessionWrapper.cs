// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class BrowseMatchSessionWrapper : MatchSessionWrapper
{
    
    private static string _nextPage;
    private static bool _isBrowseMatchSessionsCanceled;
    private static bool _isJoinMatchSessionCancelled;
    private static bool _isQueryingNextMatchSessions;
    private static Action<BrowseMatchResult> _onQueryMatchSessionFinished;
    private static Action<BrowseMatchResult> _onQueryNextPageMatchSessionFinished;
    
    private void Start()
    {
        OnBrowseMatchSessionCompleteEvent += OnBrowseMatchSessionsComplete;
    }

    #region BrowseMatchSession

    protected internal void BrowseMatch(Action<BrowseMatchResult> onSessionRetrieved)
    {
        _nextPage = "";
        _isBrowseMatchSessionsCanceled = false;
        _onQueryMatchSessionFinished = onSessionRetrieved;
        BrowseCustomMatchSession(MatchSessionConfig.CreatedMatchSessionAttribute);
    }
    
    public void CancelBrowseMatchSessions()
    {
        _isBrowseMatchSessionsCanceled = true;
    }

    #endregion

    #region QueryMatchSession

    public void QueryNextMatchSessions(Action<BrowseMatchResult> onQueryNextMatchSessionsFinished)
    {
        if (String.IsNullOrEmpty(_nextPage))
        {
            onQueryNextMatchSessionsFinished?
                .Invoke(new BrowseMatchResult(Array.Empty<SessionV2GameSession>()));
            _isQueryingNextMatchSessions = false;
        }
        else
        {
            if (!_isQueryingNextMatchSessions)
            {
                var req = GenerateRequestFromNextPage(_nextPage);
                _onQueryNextPageMatchSessionFinished = onQueryNextMatchSessionsFinished;
                _session?.QueryGameSession(req, OnQueryNextPageFinished);
            }
        }
    }

    private void OnQueryNextPageFinished(Result<PaginatedResponse<SessionV2GameSession>> result)
    {
        if (result.IsError)
        {
            _onQueryNextPageMatchSessionFinished?.Invoke(new BrowseMatchResult(null, result.Error.Message));
        }
        else
        {
            _nextPage = result.Value.paging.next;
            if(!String.IsNullOrEmpty(_nextPage))
                BytewarsLogger.Log($"next page: {_nextPage}");
            _onQueryNextPageMatchSessionFinished?.Invoke(new BrowseMatchResult(result.Value.data));
        }
        _isQueryingNextMatchSessions = false;
    }
    
    private static Dictionary<string, object> GenerateRequestFromNextPage(string nextPageUrl)
    {
        _isQueryingNextMatchSessions = true;
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
        if (!result.IsError)
        {
            if(!_isBrowseMatchSessionsCanceled)
                _onQueryMatchSessionFinished?.Invoke(new BrowseMatchResult(result.Value.data));
            _nextPage = result.Value.paging.next;
        }
        else
        {
            if(!_isBrowseMatchSessionsCanceled)
                _onQueryMatchSessionFinished?.Invoke(new BrowseMatchResult(null, result.Error.Message));
        }
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