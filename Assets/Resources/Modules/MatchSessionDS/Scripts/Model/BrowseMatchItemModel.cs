using System;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class BrowseMatchItemModel
{
    public InGameMode GameMode { get; private set; }
    public string MatchSessionId { get; private set; }
    public MatchSessionServerType SessionServerType { get; private set; }
    public string MatchCreatorName { get; private set; }
    public string MatchCreatorAvatarURL { get; private set; }
    public int MaxPlayerCount { get; private set; }
    public int CurrentPlayerCount { get; private set; }
    public Action<BrowseMatchItemModel> OnDataUpdated;
    private const string ClassName = "[BrowseMatchItemModel]";
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
            Debug.LogWarning($"{ClassName} OnPublicUserDataRetrieved error: {result.Error.Message}");
        }
        else
        {
            var publicUserData = result.Value;
            var creatorName = DefaultWarName;
            if (!String.IsNullOrEmpty(publicUserData.displayName))
            {
                creatorName = publicUserData.displayName + "'s war";
            }
            MatchCreatorName = creatorName;
            MatchCreatorAvatarURL = publicUserData.avatarUrl;
            OnDataUpdated?.Invoke(this);
        }
    }

    private int GetJoinedPlayerCount(SessionV2MemberData[] members)
    {
        int joinedMemberCount = 0;
        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];
            if (member.status == SessionV2MemberStatus.JOINED)
            {
                joinedMemberCount++;
            }
        }
        return joinedMemberCount;
    }

    private void SetMatchTypeAndServerType(SessionV2GameSession gameSession)
    {
        var gameSessionName = gameSession.configuration.name;
        if (gameSessionName.Equals(MatchSessionConfig.UnitySessionEliminationDs))
        {
            GameMode = InGameMode.CreateMatchEliminationGameMode;
            SessionServerType = MatchSessionServerType.DedicatedServer;
        }
        else if (gameSessionName.Equals(MatchSessionConfig.UnitySessionEliminationP2P))
        {
            GameMode = InGameMode.CreateMatchEliminationGameMode;
            SessionServerType = MatchSessionServerType.PeerToPeer;
        }
        else if (gameSessionName.Equals(MatchSessionConfig.UnitySessionDeathMatchDs))
        {
            GameMode = InGameMode.CreateMatchDeathMatchGameMode;
            SessionServerType = MatchSessionServerType.DedicatedServer;
        }
        else if (gameSessionName.Equals(MatchSessionConfig.UnitySessionDeathMatchP2P))
        {
            GameMode = InGameMode.CreateMatchDeathMatchGameMode;
            SessionServerType = MatchSessionServerType.PeerToPeer;
        }
    }
    
}
