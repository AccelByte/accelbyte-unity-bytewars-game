// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class GameSessionUtilityWrapper : SessionEssentialsWrapper
{
    private const string EliminationDSMatchPool = "unity-elimination-ds";
    private const string EliminationDSAMSMatchPool = "unity-elimination-ds-ams";
    private const string TeamDeathmatchDSMatchPool = "unity-teamdeathmatch-ds";
    private const string TeamDeathmatchDSAMSMatchPool = "unity-teamdeathmatch-ds-ams";

    private static Dictionary<string, BaseUserInfo> CachedGameSessionMembersInfo = new();
    private static Dictionary<string, KeyValuePair<ulong, GameManager.OnClientAuthenticationCompleteDelegate>> PlayersToAuthenticate = new();
    private static string lastAuthenticatedSessionId = string.Empty;
    private bool isAuthenticationSequenceRunning = false;

#if UNITY_SERVER
    private DedicatedServerManager dedicatedServerManager;
    
    public DedicatedServerManager DedicatedServerManager 
    { 
        get => dedicatedServerManager;
        private set => dedicatedServerManager = value; 
    }
#endif

    protected void Awake()
    {
        base.Awake();
#if UNITY_SERVER
        dedicatedServerManager = AccelByteSDK.GetServerRegistry().GetApi().GetDedicatedServerManager();
        DedicatedServerManager = dedicatedServerManager;
#endif
    }

    private void OnEnable()
    {
        GameManager.OnClientConnectedAuthentication += AuthenticatePlayer;
    }

    private void OnDisable()
    {
        GameManager.OnClientConnectedAuthentication -= AuthenticatePlayer;
    }

    #region GameClient
    protected internal void TravelToDS(SessionV2GameSession sessionV2Game, InGameMode gameMode)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            return;
        }

        GameManager.Instance.ShowTravelingLoading(() => StartClient(sessionV2Game, gameMode));
    }

    private void StartClient(SessionV2GameSession sessionV2Game, InGameMode gameMode)
    {
        ushort port = GetPort(sessionV2Game.dsInformation);
        string ip = sessionV2Game.dsInformation.server.ip;
        InitialConnectionData initialData = new InitialConnectionData()
        {
            sessionId = string.Empty,
            inGameMode = gameMode,
            serverSessionId = sessionV2Game.id,
            userId = GameData.CachedPlayerState.PlayerId
        };

        GameManager.Instance.StartAsClient(ip, port, initialData);
    }

    private ushort GetPort(SessionV2DsInformation dsInformation)
    {
        int port = ConnectionHandler.DefaultPort;
        if (dsInformation.server.ports.Count > 0)
        {
            dsInformation.server.ports.TryGetValue("default_port", out port);
        }
        if (port == 0)
        {
            port = dsInformation.server.port;
        }
        return (ushort)port;
    }
    #endregion GameClient

    #region Authentication
    private void AuthenticatePlayer(ulong userNetId, GameManager.OnClientAuthenticationCompleteDelegate onComplete)
    {
        // New session to authenticate, reset cache.
        if (GameData.ServerSessionID != lastAuthenticatedSessionId)
        {
            lastAuthenticatedSessionId = GameData.ServerSessionID;
            CachedGameSessionMembersInfo.Clear();
            PlayersToAuthenticate.Clear();
        }

        // Abort authentication if the player state is not exists.
        if (!GameManager.Instance.ConnectedPlayerStates.TryGetValue(userNetId, out PlayerState playerState))
        {
            onComplete.Invoke(userNetId, false);
            return;
        }

        string userId = playerState.PlayerId;
        BytewarsLogger.Log($"Authenticating player {userId}");

        if (AuthenticatePlayer_IsPlayerInGameSession(userId, userNetId))
        {
            onComplete.Invoke(userNetId, true);
            return;
        }

        // Add player to authentication queue.
        if (PlayersToAuthenticate.Keys.Contains(userId))
        {
            return;
        }
        PlayersToAuthenticate.Add(
            userId, 
            new KeyValuePair<ulong, GameManager.OnClientAuthenticationCompleteDelegate>(userNetId, onComplete));

        AuthenticatePlayer_RefreshGameSession();
    }

    private bool AuthenticatePlayer_IsPlayerInGameSession(string userId, ulong userNetId)
    {
        bool result = CachedGameSessionMembersInfo.ContainsKey(userId);
        BytewarsLogger.Log($"Is player {userId} is in current game session member list: {result}");

        // Update player state
        if (CachedGameSessionMembersInfo.TryGetValue(userId, out BaseUserInfo userInfo) &&
            GameManager.Instance.ConnectedPlayerStates.TryGetValue(userNetId, out PlayerState playerState))
        {
            playerState.PlayerId = userInfo.userId;
            playerState.AvatarUrl = userInfo.avatarUrl;
            playerState.PlayerName = userInfo.displayName;
            GameManager.Instance.ConnectedPlayerStates[userNetId] = playerState;
        }

        return result;
    }

    private void AuthenticatePlayer_RefreshGameSession()
    {
        if (!isAuthenticationSequenceRunning)
        {
            isAuthenticationSequenceRunning = true;

            BytewarsLogger.Log($"Refresh game session info for session id {GameData.ServerSessionID}");

            if (NetworkManager.Singleton.IsClient)
            {
                AccelByteSDK.GetClientRegistry().GetApi().GetSession().GetGameSessionDetailsBySessionId(
                    GameData.ServerSessionID, 
                    AuthenticatePlayer_OnRefreshGameSessionComplete);
            }
            else
            {
                AccelByteSDK.GetServerRegistry().GetApi().GetSession().GetGameSessionDetails(
                    GameData.ServerSessionID, 
                    AuthenticatePlayer_OnRefreshGameSessionComplete);
            }
        }
    }

    private void AuthenticatePlayer_OnRefreshGameSessionComplete(Result<SessionV2GameSession> result)
    {
        if (PlayersToAuthenticate.Count <= 0)
        {
            AuthenticatePlayer_CompleteTask(true);
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to refresh game session info complete. Error {result.Error.Code}: {result.Error.Message}");
            AuthenticatePlayer_CompleteTask(false);
            return;
        }

        BytewarsLogger.Log($"Success to refresh game session info. Continue to query game session member user info.");

        List<string> members = result.Value.members.Select(x => x.id).ToList();
        AuthenticatePlayer_QuerySessionMemberUserInfo(members);
    }

    private void AuthenticatePlayer_QuerySessionMemberUserInfo(List<string> userIds)
    {
        if (userIds.Count <= 0)
        {
            AuthenticatePlayer_CompleteTask(true);
            return;
        }

        if (NetworkManager.Singleton.IsClient)
        {
            string usersToQuery = string.Join(", ", userIds);
            BytewarsLogger.Log($"Query session member info for players: {usersToQuery}");

            AccelByteSDK.GetClientRegistry().GetApi().GetUser().GetUserOtherPlatformBasicPublicInfo(
                "ACCELBYTE", 
                userIds.ToArray(), 
                (Result<AccountUserPlatformInfosResponse> result) =>
                {
                    if (result.IsError)
                    {
                        BytewarsLogger.LogWarning($"Failed to query game session member user info for players: {usersToQuery}. Error {result.Error.Code}: {result.Error.Message}");
                        AuthenticatePlayer_CompleteTask(false);
                        return;
                    }

                    BytewarsLogger.Log($"Success to query game session member user info for players: {usersToQuery}");

                    foreach(AccountUserPlatformData userInfo in result.Value.Data)
                    {
                        CachedGameSessionMembersInfo.TryAdd(userInfo.UserId, new BaseUserInfo());
                        CachedGameSessionMembersInfo[userInfo.UserId] = new BaseUserInfo()
                        {
                            userId = userInfo.UserId,
                            displayName = userInfo.DisplayName,
                            UniqueDisplayName = userInfo.UniqueDisplayName,
                            avatarUrl = userInfo.AvatarUrl
                        };
                    }

                    userIds.Clear();
                    AuthenticatePlayer_QuerySessionMemberUserInfo(userIds);
                });
        }
        else
        {
            string userToQuery = userIds.First();
            userIds.RemoveAt(0);

            BytewarsLogger.Log($"Query session member info for player {userToQuery}");

            ServerUserAccount serverUserApi = AccelByteSDK.GetServerRegistry().GetApi().GetUserAccount();
            serverUserApi.GetUserByUserId(userToQuery, (Result<UserData> result) =>
            {
                if (result.IsError)
                {
                    BytewarsLogger.LogWarning($"Failed to query game session member user info for player {userToQuery}. Error {result.Error.Code}: {result.Error.Message}");
                    AuthenticatePlayer_CompleteTask(false);
                    return;
                }

                BytewarsLogger.Log($"Success to query game session member user info for player {userToQuery}");

                CachedGameSessionMembersInfo.TryAdd(result.Value.userId, null);
                CachedGameSessionMembersInfo[result.Value.userId] = new BaseUserInfo()
                {
                    userId = result.Value.userId,
                    displayName = result.Value.displayName,
                    UniqueDisplayName = result.Value.UniqueDisplayName,
                    avatarUrl = result.Value.avatarUrl
                };

                AuthenticatePlayer_QuerySessionMemberUserInfo(userIds);
            });
        }
    }

    private void AuthenticatePlayer_CompleteTask(bool bSucceeded)
    {
        BytewarsLogger.Log($"Authenticate player complete. Is success: {bSucceeded}");

        List<string> userIds = PlayersToAuthenticate.Keys.ToList();
        foreach (string userId in userIds)
        {
            if (!PlayersToAuthenticate.TryGetValue(
                userId, 
                out KeyValuePair<ulong, GameManager.OnClientAuthenticationCompleteDelegate> data))
            {
                continue;
            }

            ulong userNetId = data.Key;
            GameManager.OnClientAuthenticationCompleteDelegate resultCallback = data.Value;

            bool isAuthenticated = bSucceeded && AuthenticatePlayer_IsPlayerInGameSession(userId, userNetId);
            BytewarsLogger.Log($"Player {userId} is authenticated: {isAuthenticated}");

            PlayersToAuthenticate.Remove(userId);
            resultCallback.Invoke(userNetId, isAuthenticated);
        }

        if (PlayersToAuthenticate.Count > 0)
        {
            BytewarsLogger.Log($"Remaining player to authenticate {PlayersToAuthenticate.Count}. Continue player authentication.");
            AuthenticatePlayer_RefreshGameSession();
        }
        else
        {
            isAuthenticationSequenceRunning = false;
        }
    }
    #endregion
}
