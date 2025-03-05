// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;

public static class GameData
{
    public static GameModeSO GameModeSo { get; set; }
    public static PlayerState CachedPlayerState { get; set; } = new();
    public static ServerType ServerType { get; set; } = ServerType.Offline;
    public static string ServerSessionID { get; set; }
}

public enum ServerType
{
    Offline,
    OnlineDedicatedServer,
    OnlinePeer2Peer
}

/// <summary>
/// This class is used to cache session data from AccelByte SDK
/// </summary>
public static class SessionCache
{
    private static readonly Dictionary<string, SessionData> CachedSessions = new();
    private static string currentPlayerJoinedSessionId = string.Empty;
    private static bool isCreateMatchSession = false;
    public static string CurrentGameSessionId = string.Empty;
    public static void SetJoinedSessionIdAndLeaderUserId(string sessionId, string leaderId)
    {
        currentPlayerJoinedSessionId = sessionId;
        SetSessionLeaderId(sessionId, leaderId);
        isCreateMatchSession = true;
    }
    
    public static void SetJoinedSessionId(string sessionId) => currentPlayerJoinedSessionId = sessionId;

    public static void SetSessionLeaderId(string sessionId, string sessionLeaderId)
    {
        if (CachedSessions.TryGetValue(sessionId, out SessionData sessionData))
        {
            sessionData.SessionLeaderUserId = sessionLeaderId;            
            return;
        }

        SessionData newSessionData = new()
        {
            SessionId = sessionId,
            SessionLeaderUserId = sessionLeaderId
        };

        CachedSessions.Add(sessionId, newSessionData);
    }

    public static string GetJoinedSessionLeaderUserId()
    {
        if (string.IsNullOrEmpty(currentPlayerJoinedSessionId))
        {
            return string.Empty;
        }

        if (!CachedSessions.TryGetValue(currentPlayerJoinedSessionId, out SessionData sessionData))
        {
            return string.Empty;
        }
    
        return sessionData.SessionLeaderUserId;
    }

    public static bool IsSessionLeader() => GameData.CachedPlayerState.PlayerId.Equals(GetJoinedSessionLeaderUserId());
    public static bool IsCreateMatch() => isCreateMatchSession;
    public static void ClearSessionCache()
    {
        CachedSessions.Clear();
        currentPlayerJoinedSessionId = string.Empty;
        CurrentGameSessionId = string.Empty;
        isCreateMatchSession = false;
    }
}

public class SessionData
{
    public string SessionId { get; set; }
    public string SessionLeaderUserId { get; set; }
}