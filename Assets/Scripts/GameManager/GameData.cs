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
    private static string _currentPlayerJoinedSessionId;

    public static void SetJoinedSessionIdAndLeaderUserId(string sessionId, string leaderId)
    {
        _currentPlayerJoinedSessionId = sessionId;
        SetSessionLeaderId(sessionId, leaderId);
    }
    
    public static void SetJoinedSessionId(string sessionId) => _currentPlayerJoinedSessionId = sessionId;

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
        if (string.IsNullOrEmpty(_currentPlayerJoinedSessionId))
        {
            return string.Empty;
        }

        if (!CachedSessions.TryGetValue(_currentPlayerJoinedSessionId, out SessionData sessionData))
        {
            return string.Empty;
        }

        return sessionData.SessionLeaderUserId;
    }

    public static bool IsSessionLeader() => GameData.CachedPlayerState.playerId.Equals(GetJoinedSessionLeaderUserId());
}

public struct SessionData
{
    public string SessionId { get; set; }
    public string SessionLeaderUserId { get; set; }
}
