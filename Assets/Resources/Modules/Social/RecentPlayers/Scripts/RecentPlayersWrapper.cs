// Copyright (c) 2025 - 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using WebSocketSharp;

public class RecentPlayersWrapper : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();

    private Session session;
    private Lobby lobby;
    private User user;

    private void Awake()
    {
        session = ApiClient.GetSession();
        lobby = ApiClient.GetLobby();
        user = ApiClient.GetUser();
    }

    #region Player List Module

    #region Main Functions

    public void QueryPlayersListInCurrentSession(ResultCallback<List<RecentPlayerInfo>> resultCallback)
    {
        string sessionId = ResolveSessionId();
        if (string.IsNullOrEmpty(sessionId))
        {
            BytewarsLogger.LogWarning("Unable to query players list. Session Id is empty.");
            return;
        }

        session.GetGameSessionDetailsBySessionId(sessionId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    "Error querying current session members, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
                return;
            }

            StartQueryPlayerInfos(result.Value, resultCallback);
        });
    }

    private void StartQueryPlayerInfos(
        SessionV2GameSession session,
        ResultCallback<List<RecentPlayerInfo>> resultCallback)
    {
        Error error = new Error(ErrorCode.NoContent, "No data");
        
        if (session == null || session.members.Length == 0)
        {
            resultCallback?.Invoke(Result<List<RecentPlayerInfo>>.CreateError(error));
            return;
        }

        Dictionary<string, SessionV2MemberData> sessionMemberInfoById = new();
        foreach (SessionV2MemberData sessionMember in session.members)
        {
            sessionMemberInfoById.Add(sessionMember.id, sessionMember);
        }

        string[] userIds = sessionMemberInfoById.Keys.ToArray();

        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", userIds, userInfoResult =>
        {
            Dictionary<string, AccountUserPlatformData> userInfoById = new();
            if (userInfoResult != null && userInfoResult.IsError)
            {
                BytewarsLogger.LogWarning(
                    "Error getting recent player info, " +
                    $"Error Code: {userInfoResult.Error.Code} Error Message: {userInfoResult.Error.Message}");

                resultCallback?.Invoke(Result<List<RecentPlayerInfo>>.CreateError(userInfoResult.Error));
                return;
            }
            if (userInfoResult != null && !userInfoResult.IsError && userInfoResult.Value?.Data != null)
            {
                foreach (AccountUserPlatformData userData in userInfoResult.Value.Data)
                {
                    if (!string.IsNullOrEmpty(userData.UserId))
                    {
                        userInfoById[userData.UserId] = userData;
                    }
                }
            }
            
            lobby.BulkGetUserPresence(userIds, presenceResult =>
            {
                Dictionary<string, UserStatusNotif> presenceById = new();
                if (presenceResult != null && presenceResult.IsError)
                {
                    BytewarsLogger.LogWarning(
                        "Error getting recent player presence, " +
                        $"Error Code: {presenceResult.Error.Code} Error Message: {presenceResult.Error.Message}");

                    resultCallback?.Invoke(Result<List<RecentPlayerInfo>>.CreateError(presenceResult.Error));
                    return;
                }
                if (presenceResult != null && !presenceResult.IsError && presenceResult.Value?.data != null)
                {
                    foreach (UserStatusNotif status in presenceResult.Value.data)
                    {
                        if (!string.IsNullOrEmpty(status.userID))
                        {
                            presenceById[status.userID] = status;
                        }
                    }
                }
            
                List<RecentPlayerInfo> resultData = new List<RecentPlayerInfo>();
                foreach (string userId in userIds)
                {
                    sessionMemberInfoById.TryGetValue(userId, out SessionV2MemberData sessionMemberData);
                    userInfoById.TryGetValue(userId, out AccountUserPlatformData userData);
                    presenceById.TryGetValue(userId, out UserStatusNotif presenceData);

                    RecentPlayerInfo playerInfo = BuildPlayerInfo(userId, sessionMemberData.StatusV2, userData, presenceData);
                    resultData.Add(playerInfo);
                }
                resultCallback?.Invoke(Result<List<RecentPlayerInfo>>.CreateOk(resultData));
            });
        });
    }

    #endregion Main Functions

    private RecentPlayerInfo BuildPlayerInfo(
        string userId,
        SessionV2MemberStatus status,
        AccountUserPlatformData userData,
        UserStatusNotif presenceData)
    {
        string displayName = userData.DisplayName;
        if (displayName.IsNullOrEmpty())
        {
            displayName = AccelByteWarsUtility.GetDefaultDisplayNameByUserId(userId);
        }
        string activity = string.Empty;
        if (presenceData != null)
        {
            activity = DecodeActivity(presenceData.activity);
        }

        RecentPlayerInfo info = new()
        {
            UserId = userId,
            DisplayName = displayName,
            AvatarUrl = userData.AvatarUrl,
            SessionStatus = status,
            Availability = presenceData?.availability ?? UserStatus.Offline,
            Activity = activity,
            LastSeenAt = presenceData?.lastSeenAt ?? default
        };

        return info;
    }

    private string ResolveSessionId()
    {
        if (!string.IsNullOrEmpty(AccelByteWarsOnlineSession.CachedSession?.id))
        {
            return AccelByteWarsOnlineSession.CachedSession?.id;
        }

        if (!string.IsNullOrEmpty(GameData.ServerSessionID))
        {
            return GameData.ServerSessionID;
        }

        if (!string.IsNullOrEmpty(SessionCache.CurrentGameSessionId))
        {
            return SessionCache.CurrentGameSessionId;
        }

        return string.Empty;
    }

    #endregion Player List Module

    private static string DecodeActivity(string encodedActivity)
    {
        if (string.IsNullOrEmpty(encodedActivity))
        {
            return string.Empty;
        }

        try
        {
            return Uri.UnescapeDataString(encodedActivity);
        }
        catch (Exception)
        {
            return encodedActivity;
        }
    }
}
