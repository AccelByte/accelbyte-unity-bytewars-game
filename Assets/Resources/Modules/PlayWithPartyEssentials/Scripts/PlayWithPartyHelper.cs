// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayWithPartyHelper : MonoBehaviour
{
    private bool IsMatchmakingEventslistened = false;
    private PlayWithPartyEssentialsWrapper playWithPartyWrapper;

    private void Start()
    {
        playWithPartyWrapper = TutorialModuleManager.Instance.GetModuleClass<PlayWithPartyEssentialsWrapper>();

        // Match Session with Party's lobby notifications event
        PlayWithPartyEssentialsWrapper.OnMatchmakingStarted += SetupMatchmakingEvents;
        PlayWithPartyEssentialsWrapper.OnGameSessionInvitationReceived += OnInvitedToGameSession;
    }


    #region Mathcmaking with party

    private void SetupMatchmakingEvents()
    {
        IsMatchmakingEventslistened = true;
        BindMatchmakingEvents(SessionConfigurationTemplateType.DS);
        //TODO: BindMatchmakingEvents(SessionConfigurationTemplateType.P2P);
    }

    private void BindMatchmakingEvents(SessionConfigurationTemplateType sessionType)
    {
        if (sessionType is SessionConfigurationTemplateType.DS)
        {
            BytewarsLogger.Log("subscribe events from matchmaking ds");
        } 
        else
        {
            //TODO: add matchmaking P2P event handling
        }
    }

    private void TravelToDS(SessionV2GameSession session)
    {
        switch (session.matchPool)
        {
            case "unity-teamdeathmatch-ds-ams":
                break;
            case "unity-elimination-ds-ams":
                break;
        }
    }

    #endregion

    #region Match Session with Party

    private void OnJoinButtonDataSet(GameObject joinButtonGameObject)
    {
        Button joinButton = joinButtonGameObject.GetComponent<Button>();
        joinButton.enabled = true; // enable button by default

        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        if (partySession != null && GameData.CachedPlayerState.PlayerId != partySession.leaderId)
        {
            joinButton.enabled = false;
        }
    }

    private void OnJoinButtonClicked(string sessionId)
    {
        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        if (partySession != null && GameData.CachedPlayerState.PlayerId == partySession.leaderId)
        {
            foreach (SessionV2MemberData member in partySession.members.Where(x => x.StatusV2 == SessionV2MemberStatus.JOINED))
            {
                playWithPartyWrapper.InviteUserToGameSession(sessionId, member.id, null);
            }
        }
    }

    private void OnInvitedToGameSession(string sessionId)
    {
        BytewarsLogger.Log($"sessionId {sessionId}");
        if (PartyEssentialsModels.PartyHelper.CurrentPartySession != null)
        {
            playWithPartyWrapper.GetGameSessionBySessionId(sessionId, OnGetGameSessionCompleted);
        }
    }

    private void OnGetGameSessionCompleted(Result<SessionV2GameSession> result)
    {
        BytewarsLogger.Log($"Result<SessionV2GameSession> {result.ToJsonString()}");
        if (!result.IsError)
        {
            if (result.Value.configuration.type is SessionConfigurationTemplateType.DS)
            {   
                SetupMatchmakingAndCreateMatch(result);
            } 
            else
            {
                //TODO: SetupMatchmakingAndCreateMatch for P2P
            }
        }
    }

    private void SetupMatchmakingAndCreateMatch(Result<SessionV2GameSession> result)
    {
        // check if the attributes contain key for create match session
        if(result.Value.attributes.ContainsKey("cm")) 
        {
            // get InGameMode based on session's configuration name
            InGameMode currentGameMode = GetGameMode(result.Value.configuration.name);

            MatchSessionDSWrapper matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
            //matchSessionDSWrapper.JoinMatchSession(
            //    result.Value.id,
            //    currentGameMode
            //);
        } 
        else
        {
            if (!IsMatchmakingEventslistened)
            {
                BindMatchmakingEvents(result.Value.configuration.type);
            }
        }
    }

    

    private Action<string> OnJoinMatchSessionCompleted(SessionV2GameSession gameSessionData)
    {
        Debug.Log("Success joining the game session!");
        //BrowseMatchSessionEventListener.OnUpdate?.Invoke(gameSessionData);
        return null;
    }

    #endregion

    private InGameMode GetGameMode(string configurationName)
    {
        switch (configurationName)
        {
            case AccelByteWarsOnlineSessionModels.EliminationDSSessionTemplateName or AccelByteWarsOnlineSessionModels.EliminationP2PSessionTemplateName or AccelByteWarsOnlineSessionModels.EliminationDSAMSSessionTemplateName:
                return InGameMode.CreateMatchElimination;
            case AccelByteWarsOnlineSessionModels.TeamDeathmatchDSSessionTemplateName or AccelByteWarsOnlineSessionModels.TeamDeathmatchP2PSessionTemplateName or AccelByteWarsOnlineSessionModels.TeamDeathmatchDSAMSSessionTemplateName:
                return InGameMode.CreateMatchTeamDeathmatch;
            default:
                return InGameMode.None;
        }
    }
}
