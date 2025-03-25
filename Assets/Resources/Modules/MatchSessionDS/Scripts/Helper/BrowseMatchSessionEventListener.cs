// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public static class BrowseMatchSessionEventListener
{
    private static Lobby lobby;
    public static Action<SessionV2GameSession> OnUpdate;
    private static List<SessionV2GameSession> displayedGameSessions;
    public static void Init(List<SessionV2GameSession> displayedGameSessions)
    {
        BrowseMatchSessionEventListener.displayedGameSessions = displayedGameSessions;
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
        lobby.SessionV2GameSessionUpdated += OnV2GameSessionUpdated;
        lobby.SessionV2UserRejectedGameSessionInvitation += OnV2UserRejectedGameSessionInvitation;        
    }

    private static void OnGameSessionUpdated(SessionV2GameSession updatedGameSession)
    {
        SessionV2GameSession updated = displayedGameSessions
            .Find(d => d.id.Equals(updatedGameSession.id));
        if (updated != null)
        {
            updated = updatedGameSession;
            OnUpdate?.Invoke(updated);
        }
    }

    private static void OnLeaveFromParty(Result<LeaveNotification> result)
    {
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2DSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (!result.IsError)
        {
            var value = result.Value;
            var updatedData = displayedGameSessions
                .Find(s => s.id.Equals(value.sessionId));
            if (updatedData != null)
            {
                updatedData = value.session;
                OnUpdate?.Invoke(updatedData);
            }
        }
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2InvitedUserToGameSession(Result<SessionV2GameInvitationNotification> result)
    {
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2UserRejectedGameSessionInvitation(Result<SessionV2GameInvitationRejectedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2GameSession updatedGameSession = displayedGameSessions
                .Find(d => d.id.Equals(result.Value.sessionId));
            if (updatedGameSession != null)
            {
                updatedGameSession.members = result.Value.members;
                OnUpdate?.Invoke(updatedGameSession);
            }
        }
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2UserKickedFromGameSession(Result<SessionV2GameUserKickedNotification> result)
    {
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2UserJoinedGameSession(Result<SessionV2GameJoinedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2GameSession updated = displayedGameSessions
                .Find(d => d.id.Equals(result.Value.sessionId));
            if (updated != null)
            {
                updated.members = result.Value.members;
                OnUpdate?.Invoke(updated);
            }
        }
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2GameSessionMemberChanged(Result<SessionV2GameMembersChangedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2GameMembersChangedNotification value = result.Value;
            SessionV2GameSession updated = displayedGameSessions
                .Find(d => d.id.Equals(value.session.id));
            if (updated != null)
            {
                updated = value.session;
                OnUpdate?.Invoke(updated);
            }
        }
        MatchSessionHelper.LogResult(result);
    }

    private static void OnV2GameSessionUpdated(Result<SessionV2GameSessionUpdatedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2GameSessionUpdatedNotification value = result.Value;
            SessionV2GameSession updated = displayedGameSessions
                .Find(d => d.id.Equals(value.id));
            if (updated != null)
            {
                updated.members = value.members;
                updated.attributes = value.attributes;
                updated.configuration = value.configuration;
                updated.teams = value.teams;
                updated.version = value.version;
                updated.createdAt = value.createdAt;
                updated.dsInformation = value.dsInformation;
                updated.matchPool = value.matchPool;
                updated.ticketIds = value.ticketIds;
                OnUpdate?.Invoke(updated);
            }
        }
        MatchSessionHelper.LogResult(result);
    }
}
