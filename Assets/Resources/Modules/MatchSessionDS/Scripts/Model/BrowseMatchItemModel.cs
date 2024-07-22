using System;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class BrowseMatchItemModel
{
    public InGameMode GameMode { get; private set; }
    public string MatchSessionId { get; private set; }
    public GameSessionServerType SessionServerType { get; private set; }
    public string MatchCreatorName { get; private set; }
    public string MatchCreatorAvatarURL { get; private set; }
    public int MaxPlayerCount { get; private set; }
    public int CurrentPlayerCount { get; private set; }
    public Action<BrowseMatchItemModel> OnDataUpdated;
    private const string DefaultWarName = "player's match";

    public BrowseMatchItemModel(SessionV2GameSession gameSession, int index = -1)
    {
        MatchSessionId = gameSession.id;
        if (index == -1)
        {
            MatchCreatorName = DefaultWarName;
        }
        else
        {
            MatchCreatorName = "war "+index;
        }
        SetPlayerCount(gameSession);
        SetMatchTypeAndServerType(gameSession);
        BrowseMatchSessionWrapper.GetUserDisplayName(gameSession.createdBy, OnPublicUserDataRetrieved);
    }

    private void SetPlayerCount(SessionV2GameSession gameSession)
    {
        MaxPlayerCount = gameSession.configuration.maxPlayers;
        CurrentPlayerCount = GetJoinedPlayerCount(gameSession.members);
    }

    public void Update(SessionV2GameSession updatedModel)
    {
        SetPlayerCount(updatedModel);
        OnDataUpdated?.Invoke(this);
    }

    private void OnPublicUserDataRetrieved(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }
        else
        {
            PublicUserData publicUserData = result.Value;
            string truncatedUserId = publicUserData.userId[..5];
            string displayName = string.IsNullOrEmpty(publicUserData.displayName) ?
                $"Player-{truncatedUserId}" : publicUserData.displayName;
            MatchCreatorName = $"{displayName}'s match";
            MatchCreatorAvatarURL = publicUserData.avatarUrl;
            OnDataUpdated?.Invoke(this);
        }
    }

    private int GetJoinedPlayerCount(SessionV2MemberData[] members)
    {
        int joinedMemberCount = 0;
        for (int i = 0; i < members.Length; i++)
        {
            SessionV2MemberData member = members[i];
            if (member.status == SessionV2MemberStatus.JOINED)
            {
                joinedMemberCount++;
            }
        }
        return joinedMemberCount;
    }

    private void SetMatchTypeAndServerType(SessionV2GameSession gameSession)
    {
        string gameSessionName = gameSession.configuration.name;
        if (gameSessionName.Equals(GameSessionConfig.UnitySessionEliminationDS))
        {
            GameMode = InGameMode.CreateMatchEliminationGameMode;
            SessionServerType = GameSessionServerType.DedicatedServer;
        }
        else if (gameSessionName.Equals(GameSessionConfig.UnitySessionEliminationP2P))
        {
            GameMode = InGameMode.CreateMatchEliminationGameMode;
            SessionServerType = GameSessionServerType.PeerToPeer;
        }
        else if (gameSessionName.Equals(GameSessionConfig.UnitySessionTeamDeathmatchDS))
        {
            GameMode = InGameMode.CreateMatchDeathMatchGameMode;
            SessionServerType = GameSessionServerType.DedicatedServer;
        }
        else if (gameSessionName.Equals(GameSessionConfig.UnitySessionTeamDeathmatchP2P))
        {
            GameMode = InGameMode.CreateMatchDeathMatchGameMode;
            SessionServerType = GameSessionServerType.PeerToPeer;
        }
        else if (gameSessionName.Equals(GameSessionConfig.UnitySessionEliminationDSAMS))
        {
            GameMode = InGameMode.CreateMatchEliminationGameMode;
            SessionServerType = GameSessionServerType.DedicatedServerAMS;
        }
        else if (gameSessionName.Equals(GameSessionConfig.UnitySessionTeamDeathmatchDSAMS))
        {
            GameMode = InGameMode.CreateMatchDeathMatchGameMode;
            SessionServerType = GameSessionServerType.DedicatedServerAMS;
        }
    }
    
}
