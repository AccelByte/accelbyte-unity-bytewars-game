// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using static AccelByteWarsOnlineSessionModels;
using static SessionEssentialsModels;
using static MatchSessionEssentialsModels;

public class MatchSessionDSWrapper : MatchSessionEssentialsWrapper
{
    protected override void Awake()
    {
        base.Awake();

        BrowseMatchMenu.OnBrowseDSMatch += BrowseGameSessions;
        Lobby.SessionV2DsStatusChanged += (result) => OnDSStatusChanged?.Invoke(result);
    }

    public override void CreateGameSession(
        SessionV2GameSessionCreateRequest request,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        // Add local server name.
        if (string.IsNullOrEmpty(ConnectionHandler.LocalServerName))
        {
            request.serverName = ConnectionHandler.LocalServerName;
        }

        // Add client version
        request.clientVersion = AccelByteWarsOnlineSessionModels.ClientVersion;

        // Add preferred regions.
        string[] preferredRegions = 
            RegionPreferencesModels.GetEnabledRegions().OrderBy(x => x.Latency).Select(x => x.RegionCode).ToArray();
        if (preferredRegions.Length > 0)
        {
            request.requestedRegions = preferredRegions;
        }

        // Add party session id for playing with party feature.
        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        if (partySession != null && !string.IsNullOrEmpty(partySession.id))
        {
            request.members = partySession.members;
        }

        // Add custom attribute as filter for session browser.
        request.attributes = MatchSessionDSAttribute;

        // Reregister delegate to listen for dedicated server status changed event.
        OnDSStatusChanged -= OnDSStatusChangedReceived;
        OnDSStatusChanged += OnDSStatusChangedReceived;

        base.CreateGameSession(request, (result) =>
        {
            // If dedicated server is ready, broadcast the dedicated server status changed event.
            if (!result.IsError && result.Value.dsInformation.StatusV2 == SessionV2DsStatus.AVAILABLE)
            {
                OnDSStatusChanged?.Invoke(Result<SessionV2DsStatusUpdatedNotification>.CreateOk(new()
                {
                    session = result.Value,
                    sessionId = result.Value.id,
                    error = string.Empty
                }));
            }

            onComplete?.Invoke(result);
        });
    }

    public override void JoinGameSession(
        string sessionId,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        // Reregister delegate to listen for dedicated server status changed event.
        OnDSStatusChanged -= OnDSStatusChangedReceived;
        OnDSStatusChanged += OnDSStatusChangedReceived;

        base.JoinGameSession(sessionId, (result) =>
        {
            // If dedicated server is ready, broadcast the dedicated server status changed event.
            if (!result.IsError && result.Value.dsInformation.StatusV2 == SessionV2DsStatus.AVAILABLE)
            {
                OnDSStatusChanged?.Invoke(Result<SessionV2DsStatusUpdatedNotification>.CreateOk(new()
                {
                    session = result.Value,
                    sessionId = result.Value.id,
                    error = string.Empty
                }));
            }

            onComplete?.Invoke(result);
        });
    }

    public override void BrowseGameSessions(
        string pageUrl,
        ResultCallback<BrowseSessionResult> onComplete)
    {
        // If provided, try parse the pagination URL parameters and add them to the query attributes.
        Dictionary<string, object> queryAttributes = new(MatchSessionDSAttribute);
        if (!string.IsNullOrEmpty(pageUrl))
        {
            Dictionary<string, object> queryParams = ParseQuerySessionParams(pageUrl);
            if (queryParams == null)
            {
                BytewarsLogger.LogWarning($"Failed to find game sessions. Pagination URL is invalid.");
                onComplete?.Invoke(Result<BrowseSessionResult>.CreateError(ErrorCode.InvalidArgument, InvalidSessionPaginationMessage));
                return;
            }
            queryParams.ToList().ForEach(x => queryAttributes[x.Key] = x.Value);
        }

        // Query game sessions and filter them based on the custom attributes.
        Session.QueryGameSession(queryAttributes, sessionResult =>
        {
            if (sessionResult.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Failed to query game sessions. " +
                    $"Error {sessionResult.Error.Code}: {sessionResult.Error.Message}");
                onComplete?.Invoke(Result<BrowseSessionResult>.CreateError(sessionResult.Error));
                return;
            }

            BytewarsLogger.Log("Success to query game sessions.");

            // Filter based on the enabled preferred regions.
            List<SessionV2GameSession> filteredSessions = 
                RegionPreferencesModels.FilterEnabledRegionGameSession(sessionResult.Value.data.ToList());

            // Return immediately if result is empty.
            if (filteredSessions.Count <= 0)
            {
                onComplete?.Invoke(Result<BrowseSessionResult>.CreateOk(
                    new BrowseSessionResult(
                        queryAttributes,
                        new PaginatedResponse<BrowseSessionModel>()
                        {
                            data = Array.Empty<BrowseSessionModel>(),
                            paging = sessionResult.Value.paging
                        })));
                return;
            }

            // Query the session owner user information.
            string[] sessionOwners = filteredSessions.Select(x => x.createdBy).ToArray();
            User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", sessionOwners, (userResult) =>
            {
                if (userResult.IsError)
                {
                    BytewarsLogger.LogWarning(
                        $"Failed to query game sessions. " +
                        $"Error {userResult.Error.Code}: {userResult.Error.Message}");
                    onComplete?.Invoke(Result<BrowseSessionResult>.CreateError(userResult.Error));
                    return;
                }

                // Construct session results.
                List<BrowseSessionModel> result = new();
                foreach(SessionV2GameSession session in filteredSessions)
                {
                    AccountUserPlatformData ownerData = userResult.Value.Data.FirstOrDefault(x => x.UserId == session.createdBy);
                    if (session != null && ownerData != null) 
                    {
                        result.Add(new(session, ownerData));
                    }
                }

                // Return results.
                onComplete?.Invoke(Result<BrowseSessionResult>.CreateOk(
                    new BrowseSessionResult(
                        queryAttributes,
                        new PaginatedResponse<BrowseSessionModel>()
                        {
                            data = result.ToArray(),
                            paging = sessionResult.Value.paging
                        })));
            });
        });
    }

    private void OnDSStatusChangedReceived(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            OnDSStatusChanged -= OnDSStatusChangedReceived;
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        SessionV2GameSession session = result.Value.session;
        SessionV2DsInformation dsInfo = session.dsInformation;

        // Check if the requested game mode is supported.
        InGameMode requestedGameMode = GetGameSessionGameMode(session);
        if (requestedGameMode == InGameMode.None)
        {
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Session's game mode is not supported by the game.");
            OnDSStatusChanged -= OnDSStatusChangedReceived;
            OnDSStatusChanged?.Invoke(
                Result<SessionV2DsStatusUpdatedNotification>.
                CreateError(ErrorCode.NotAcceptable, InvalidSessionTypeMessage));
            return;
        }

        // Check the dedicated server status.
        switch (dsInfo.StatusV2)
        {
            case SessionV2DsStatus.AVAILABLE:
                OnDSStatusChanged -= OnDSStatusChangedReceived;
                TravelToDS(session, requestedGameMode);
                break;
            case SessionV2DsStatus.FAILED_TO_REQUEST:
            case SessionV2DsStatus.ENDED:
            case SessionV2DsStatus.UNKNOWN:
                OnDSStatusChanged -= OnDSStatusChangedReceived;
                BytewarsLogger.LogWarning(
                    $"Failed to handle dedicated server status changed event. " +
                    $"Session failed to request for dedicated server due to unknown reason.");
                break;
            default:
                BytewarsLogger.Log($"Received dedicated server status change. Status: {dsInfo.StatusV2}");
                break;
        }
    }
}
