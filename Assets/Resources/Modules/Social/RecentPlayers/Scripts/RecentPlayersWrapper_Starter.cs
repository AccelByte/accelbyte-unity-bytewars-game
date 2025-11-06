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

public class RecentPlayersWrapper_Starter : MonoBehaviour
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
        // TODO: Implement Query Player List function here.
        BytewarsLogger.LogWarning("QueryPlayersListInCurrentSession is not yet implemented.");
    }

    private void StartQueryPlayerInfos(
        SessionV2GameSession session,
        ResultCallback<List<RecentPlayerInfo>> resultCallback)
    {
        // TODO: Implement Start Query Player Infos function here.
        BytewarsLogger.LogWarning("StartQueryPlayerInfos is not yet implemented.");
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

        return "";
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
