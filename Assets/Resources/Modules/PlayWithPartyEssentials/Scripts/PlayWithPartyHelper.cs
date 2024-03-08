// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayWithPartyHelper : MonoBehaviour
{
    private PlayWithPartyEssentialsWrapper playWithPartyWrapper;
    private AuthEssentialsWrapper authWrapper;

    private void Start()
    {
        playWithPartyWrapper = TutorialModuleManager.Instance.GetModuleClass<PlayWithPartyEssentialsWrapper>();
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        // On Match Session's Join Button clicked event
        MatchSessionItem.OnJoinButtonDataSet += OnJoinButtonDataSet;
        MatchSessionItem.OnJoinButtonClicked += OnJoinButtonClicked;

        // Match Session with Party's lobby notifications event
        PlayWithPartyEssentialsWrapper.OnGameSessionInvitationReceived += OnInvitedToGameSession;
    }

    #region Match Session with Party

    private void OnJoinButtonDataSet(GameObject joinButtonGameObject)
    {
        Button joinButton = joinButtonGameObject.GetComponent<Button>();
        joinButton.enabled = true; // enable button by default
        if (!String.IsNullOrWhiteSpace(PartyHelper.CurrentPartyId)
            && authWrapper.userData.user_id != PartyHelper.CurrentLeaderUserId)
        {
            joinButton.enabled = false; // disable if player is a party member
        }
    }

    private void OnJoinButtonClicked(string sessionId)
    {
        if (authWrapper.userData.user_id == PartyHelper.CurrentLeaderUserId)
        {
            foreach (PartyMemberData member in PartyHelper.PartyMembersData)
            {
                playWithPartyWrapper.InviteUserToGameSession(sessionId, member.UserId, null);
            }
        }
    }

    private void OnInvitedToGameSession(string sessionId)
    {
        BytewarsLogger.Log($"sessionId {sessionId}");
        if (!String.IsNullOrWhiteSpace(PartyHelper.CurrentPartyId))
        {
            playWithPartyWrapper.GetGameSessionBySessionId(sessionId, OnGetGameSessionCompleted);
        }
    }

    private void OnGetGameSessionCompleted(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"Result<SessionV2GameSession> {result.ToJsonString()}");
        if (!result.IsError)
        {
            // get InGameMode based on session's configuration name
            InGameMode currentGameMode = GetGameMode(result.Value.configuration.name);

            if (result.Value.configuration.type is SessionConfigurationTemplateType.DS)
            {
                MatchSessionDSWrapper matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
                matchSessionDSWrapper.JoinMatchSession(
                    result.Value.id,
                    currentGameMode,
                    OnJoinMatchSessionCompleted(result.Value)
                );
            }
        }
    }

    private Action<string> OnJoinMatchSessionCompleted(SessionV2GameSession gameSessionData)
    {
        Debug.Log("Success joining the game session!");
        BrowseMatchSessionEventListener.OnUpdate?.Invoke(gameSessionData);
        return null;
    }

    #endregion

    private InGameMode GetGameMode(string configurationName)
    {
        switch (configurationName)
        {
            case MatchSessionConfig.UnitySessionEliminationDs or MatchSessionConfig.UnitySessionEliminationP2P:
                return InGameMode.CreateMatchEliminationGameMode;
            case MatchSessionConfig.UnitySessionDeathMatchDs or MatchSessionConfig.UnitySessionDeathMatchP2P:
                return InGameMode.CreateMatchDeathMatchGameMode;
            default:
                return InGameMode.None;
        }
    }
}
