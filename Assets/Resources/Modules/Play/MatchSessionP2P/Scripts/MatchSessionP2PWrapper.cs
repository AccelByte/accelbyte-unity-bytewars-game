// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
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

public class MatchSessionP2PWrapper : MatchSessionEssentialsWrapper
{
    protected override void Awake()
    {
        base.Awake();

        BrowseMatchMenu.OnBrowseP2PMatch += BrowseGameSessions;
    }

    public override void CreateGameSession(
        SessionV2GameSessionCreateRequest request,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        // Add party session id for playing with party feature.
        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        if (partySession != null && !string.IsNullOrEmpty(partySession.id))
        {
            request.members = partySession.members;
        }

        // Add custom attribute as filter for session browser.
        request.attributes = MatchSessionP2PAttribute;

        base.CreateGameSession(request, (result =>
        {
            if (!result.IsError)
            {
                InGameMode requestedGameMode = GetGameSessionGameMode(result.Value);
                if (requestedGameMode == InGameMode.None)
                {
                    BytewarsLogger.LogWarning($"Failed to travel to the P2P host. Session's game mode is not supported by the game.");
                    onComplete?.Invoke(Result<SessionV2GameSession>.CreateError(ErrorCode.NotAcceptable, InvalidSessionTypeMessage));
                    return;
                }
                TravelToP2PHost(result.Value, requestedGameMode);
            }

            onComplete?.Invoke(result);
        }));
    }

    public override void JoinGameSession(
        string sessionId,
        ResultCallback<SessionV2GameSession> onComplete)
    {
        base.JoinGameSession(sessionId, (result) =>
        {
            if (!result.IsError)
            {
                InGameMode requestedGameMode = GetGameSessionGameMode(result.Value);
                if (requestedGameMode == InGameMode.None)
                {
                    BytewarsLogger.LogWarning($"Failed to travel to the P2P host. Session's game mode is not supported by the game.");
                    onComplete?.Invoke(Result<SessionV2GameSession>.CreateError(ErrorCode.NotAcceptable, InvalidSessionTypeMessage));
                    return;
                }
                TravelToP2PHost(result.Value, requestedGameMode);
            }

            onComplete?.Invoke(result);
        });
    }

    public override void BrowseGameSessions(
        string pageUrl,
        ResultCallback<BrowseSessionResult> onComplete)
    {
        // If provided, try parse the pagination URL parameters and add them to the query attributes.
        Dictionary<string, object> queryAttributes = new(MatchSessionP2PAttribute);
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

            // Return immediately if result is empty.
            if (sessionResult.Value.data.Length <= 0)
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
            string[] sessionOwners = sessionResult.Value.data.Select(x => x.createdBy).ToArray();
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
                foreach (SessionV2GameSession session in sessionResult.Value.data)
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
}
