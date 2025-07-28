// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using AccelByte.Models;
using System.Collections.Generic;
using static AccelByteWarsOnlineSessionModels;

public class MatchSessionEssentialsModels
{
    public static readonly string MatchSessionAttributeKey = "match_session_type";
    public static readonly string MatchSessionDSAttributeValue = "unity_match_session_ds";
    public static readonly string MatchSessionP2PAttributeValue = "unity_match_session_p2p";

    public static readonly Dictionary<string, object> MatchSessionDSAttribute = new()
    {
        { MatchSessionAttributeKey, MatchSessionDSAttributeValue }
    };

    public static readonly Dictionary<string, object> MatchSessionP2PAttribute = new()
    {
        { MatchSessionAttributeKey, MatchSessionP2PAttributeValue }
    };

    public static Dictionary<string, object> ParseQuerySessionParams(string pageUrl)
    {
        if (string.IsNullOrEmpty(pageUrl))
        {
            return null;
        }

        // Abort due to malformed URL. The URL does not contain parameters.
        string[] urlParts = pageUrl.Split('?');
        if (urlParts.Length < 2)
        {
            return null;
        }

        // Parse the parameters.
        Dictionary<string, object> result = new();
        string[] parameters = urlParts[1].Split('&');
        foreach (string param in parameters)
        {
            string[] paramParts = param.Split('=');

            // Abort due to malformed URL. The parameter does not contain value.
            if (paramParts.Length < 2)
            {
                return null;
            }

            // Add parameter to the query attribute.
            result.Add(paramParts[0], paramParts[1]);
        }

        return result;
    }

    public enum MatchSessionMenuState
    {
        CreateMatch,
        JoinMatch,
        JoinedMatch,
        LeaveMatch,
        RequestingServer,
        Error
    };

    public class BrowseSessionModel
    {
        public SessionV2GameSession Session { get; private set; }
        public AccountUserPlatformData Owner { get; private set; }
        public int CurrentMemberCount { get; private set; }
        public int MaxMemberCount { get; private set; }
        public InGameMode GameMode { get; private set; }
        public GameSessionServerType ServerType { get; private set; }

        public BrowseSessionModel() 
        {
            Session = null;
            Owner = null;

            CurrentMemberCount = 0;
            MaxMemberCount = 0;

            GameMode = InGameMode.None;
            ServerType = GameSessionServerType.None;
        }

        public BrowseSessionModel(SessionV2GameSession session, AccountUserPlatformData owner)
        {
            Session = session;
            Owner = owner;

            CurrentMemberCount = session.members.Count(member => member.status == SessionV2MemberStatus.JOINED);
            MaxMemberCount = session.configuration.maxPlayers;

            GameMode = GetGameSessionGameMode(session);
            ServerType = GetGameSessionServerType(session);
        }
    }

    public class BrowseSessionResult
    {
        public readonly Dictionary<string, object> QueryAttribute;
        public readonly PaginatedResponse<BrowseSessionModel> Result;

        public BrowseSessionResult(
            Dictionary<string, object> queryAttribute,
            PaginatedResponse<BrowseSessionModel> result)
        {
            QueryAttribute = queryAttribute;
            Result = result;
        }
    }
}
