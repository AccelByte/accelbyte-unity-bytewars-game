// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayingWithPartyWrapper : SessionEssentialsWrapper
{
    private UnityAction<Scene, LoadSceneMode> onSceneLoaded;
    private Action<MenuCanvas> onMenuChanged;

    private bool cachedInGameStatus = false;

    private void OnEnable()
    {
        OnValidateToStartMatchmaking = IsValidToStartPartyMatchmaking;
        OnValidateToStartGameSession = IsValidToStartPartyGameSession;
        OnValidateToJoinGameSession = IsValidToJoinPartyGameSession;

        onSceneLoaded = (scene, mode) => UpdatePartyMemberGameSessionStatus();
        onMenuChanged = (menu) => UpdatePartyMemberGameSessionStatus();
        SceneManager.sceneLoaded += onSceneLoaded;
        MenuManager.OnMenuChanged += onMenuChanged;

        if (Lobby != null)
        {
            // Bind party matchmaking events.
            Lobby.MatchmakingV2MatchmakingStarted += OnPartyMatchmakingStarted;
            Lobby.MatchmakingV2MatchFound += OnPartyMatchmakingFound;
            Lobby.MatchmakingV2MatchmakingCanceled += OnPartyMatchmakingCanceled;
            Lobby.MatchmakingV2TicketExpired += OnPartyMatchmakingExpired;

            // Bind game session events.
            Lobby.SessionV2InvitedUserToGameSession += OnInvitedToPartyGameSession;
            Lobby.SessionV2UserJoinedGameSession += OnPartyGameSessionJoined;
        }
    }

    private void OnDisable()
    {
        OnValidateToStartMatchmaking = gameMode => UniTask.FromResult(true);
        OnValidateToStartGameSession = () => UniTask.FromResult(true);
        OnValidateToJoinGameSession = sessionId => UniTask.FromResult(true);

        SceneManager.sceneLoaded -= onSceneLoaded;
        MenuManager.OnMenuChanged -= onMenuChanged;

        if (Lobby != null)
        {
            // Unbind party matchmaking events.
            Lobby.MatchmakingV2MatchmakingStarted -= OnPartyMatchmakingStarted;
            Lobby.MatchmakingV2MatchFound -= OnPartyMatchmakingFound;
            Lobby.MatchmakingV2MatchmakingCanceled -= OnPartyMatchmakingCanceled;
            Lobby.MatchmakingV2TicketExpired -= OnPartyMatchmakingExpired;

            // Unbind game session events.
            Lobby.SessionV2InvitedUserToGameSession -= OnInvitedToPartyGameSession;
            Lobby.SessionV2UserJoinedGameSession -= OnPartyGameSessionJoined;
        }
    }

    private void OnPartyMatchmakingStarted(Result<MatchmakingV2MatchmakingStartedNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to start party matchmaking. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Party matchmaking started.");

        /* Show notification that the party matchmaking is started.
         * Only show the notification if the player is a party member.*/
        if (!IsPartyLeader)
        {
            MenuManager.Instance.PromptMenu.ShowLoadingPrompt(PlayingWithPartyModels.PartyMatchmakingStartedMessage);
        }
    }

    private void OnPartyMatchmakingFound(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to party matchmaking. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }
        else
        {
            BytewarsLogger.Log($"Party matchmaking found. Currently joining the match.");
        }

        /* Show notification that the party matchmaking is completed.
	     * Only show the notification if the player is a party member.*/
        if (!IsPartyLeader)
        {
            if (result.IsError)
            {
                MenuManager.Instance.PromptMenu.HidePromptMenu();
                MenuManager.Instance.PushNotification(new PushNotificationModel()
                {
                    Message = PlayingWithPartyModels.PartyMatchmakingFailedMessage,
                    UseDefaultIconOnEmpty = false
                });
            }
            else
            {
                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(PlayingWithPartyModels.PartyMatchmakingSuccessMessage);
            }
        }
    }

    private void OnPartyMatchmakingCanceled(Result<MatchmakingV2MatchmakingCanceledNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to cancel party matchmaking. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Party matchmaking canceled.");

        /* Show notification that the party matchmaking is canceled.
	     * Only show the notification if the player is a party member.*/
        if (!IsPartyLeader)
        {
            MenuManager.Instance.PromptMenu.HidePromptMenu();
            MenuManager.Instance.PushNotification(new PushNotificationModel()
            {
                Message = PlayingWithPartyModels.PartyMatchmakingCanceledMessage,
                UseDefaultIconOnEmpty = false
            });
        }
    }

    private void OnPartyMatchmakingExpired(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle party matchmaking expired event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Party matchmaking expired.");

        if (!IsPartyLeader)
        {
            MenuManager.Instance.PromptMenu.HidePromptMenu();
            MenuManager.Instance.PushNotification(new PushNotificationModel()
            {
                Message = PlayingWithPartyModels.PartyMatchmakingExpiredMessage,
                UseDefaultIconOnEmpty = false
            });
        }
    }

    private void OnInvitedToPartyGameSession(Result<SessionV2GameInvitationNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle party game session invitation event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Received party game session session invitation.");

        /* If the player is a party member, join the party leader's game session
         * only if the player is not currently in any session, including offline single-player mode. */
        if (GameData.GameModeSo?.GameMode != GameModeEnum.MainMenu && 
            CachedSession == null && !IsPartyLeader)
        {
            Session.GetGameSessionDetailsBySessionId(result.Value.sessionId, (sessionResult) =>
            {
                if (sessionResult.IsError)
                {
                    BytewarsLogger.LogWarning($"Failed to get game session details. Error {sessionResult.Error.Code}: {sessionResult.Error.Message}");
                    return;
                }

                // If the session is not from party leader, ignore it.
                if (!sessionResult.Value.members.Select(m => m.id).Contains(CachedParty?.leaderId))
                {
                    return;
                }

                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(PlayingWithPartyModels.JoinPartyGameSessionMessage);

                JoinGameSession(sessionResult.Value.id, (joinResult) =>
                {
                    if (joinResult.IsError)
                    {
                        BytewarsLogger.LogWarning($"Failed to join party leader game session. Error {joinResult.Error.Code}: {joinResult.Error.Message}");
                        MenuManager.Instance.PromptMenu.HidePromptMenu();
                        MenuManager.Instance.PushNotification(new PushNotificationModel()
                        {
                            Message = PlayingWithPartyModels.JoinPartyGameSessionFailedMessage
                        });
                        return;
                    }

                    // Handle travel to server or host.
                    SessionV2GameSession gameSession = joinResult.Value;
                    switch (gameSession.configuration.type)
                    {
                        case SessionConfigurationTemplateType.DS:
                            if (gameSession.dsInformation == null || gameSession.dsInformation.status != SessionV2DsStatus.AVAILABLE)
                            {
                                Lobby.SessionV2DsStatusChanged += OnDSStatusChangedReceived;
                                MenuManager.Instance.PromptMenu.ShowLoadingPrompt(
                                    PlayingWithPartyModels.JoinPartyGameSessionWaitServerMessage, true, 
                                    PromptMenuCanvas.DefaultCancelMessage, () =>
                                    {
                                        Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
                                        LeaveGameSession(gameSession.id, null);
                                    });
                            }
                            else
                            {
                                TravelToDS(gameSession, AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(gameSession));
                            }
                            break;
                        case SessionConfigurationTemplateType.P2P:
                            TravelToP2PHost(gameSession, AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(gameSession));
                            break;
                        default:
                            BytewarsLogger.LogWarning($"Failed to travel. Unsupported game session server configuration type.");
                            break;
                    }
                });
            });
        }
    }

    private void OnPartyGameSessionJoined(Result<SessionV2GameJoinedNotification> result)
    {
        if (!IsInParty)
        {
            return;
        }

        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle party game session joined event. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Received game session joined event.");

        if (IsPartyLeader)
        {
            HashSet<string> partyMemberIds = CachedParty?.members.Where(m => m.StatusV2 == SessionV2MemberStatus.JOINED).Select(m => m.id).ToHashSet();
            HashSet<string> sessionMemberIds = result.Value.members.Select(m => m.id).ToHashSet();

            // Send manual invites to party members not yet invited.
            string[] uninvitedPartyMemberIds = partyMemberIds?.Where(id => !sessionMemberIds.Contains(id)).ToArray();
            if (uninvitedPartyMemberIds.Length > 0)
            {
                foreach (string memberId in uninvitedPartyMemberIds)
                {
                    SendGameSessionInvite(result.Value.sessionId, memberId, null);
                }
            }
        }
    }

    private void OnDSStatusChangedReceived(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Error {result.Error.Code}: {result.Error.Message}");
            Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
            MenuManager.Instance.PromptMenu.ShowPromptMenu(
                PromptMenuCanvas.DefaultErrorPromptMessage,
                SessionEssentialsModels.FailedToFindServerMessage,
                PromptMenuCanvas.DefaultOkMessage, null);
            return;
        }

        SessionV2GameSession session = result.Value.session;
        SessionV2DsInformation dsInfo = session.dsInformation;

        // Check if the requested game mode is supported.
        InGameMode requestedGameMode = AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(session);
        if (requestedGameMode == InGameMode.None)
        {
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Session's game mode is not supported by the game.");
            Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
            MenuManager.Instance.PromptMenu.ShowPromptMenu(
                PromptMenuCanvas.DefaultErrorPromptMessage,
                SessionEssentialsModels.FailedToFindServerMessage,
                PromptMenuCanvas.DefaultOkMessage, null);
            return;
        }

        if (dsInfo == null)
        {
            BytewarsLogger.LogWarning(
                $"Failed to handle dedicated server status changed event. " +
                $"Dedicated server information not found.");
            Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
            MenuManager.Instance.PromptMenu.ShowPromptMenu(
                PromptMenuCanvas.DefaultErrorPromptMessage,
                SessionEssentialsModels.FailedToFindServerMessage,
                PromptMenuCanvas.DefaultOkMessage, null);
            return;
        }

        // Check the dedicated server status.
        switch (dsInfo.StatusV2)
        {
            case SessionV2DsStatus.AVAILABLE:
                Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
                TravelToDS(session, requestedGameMode);
                break;
            case SessionV2DsStatus.FAILED_TO_REQUEST:
            case SessionV2DsStatus.ENDED:
            case SessionV2DsStatus.UNKNOWN:
                Lobby.SessionV2DsStatusChanged -= OnDSStatusChangedReceived;
                BytewarsLogger.LogWarning(
                    $"Failed to handle dedicated server status changed event. " +
                    $"Session failed to request for dedicated server due to unknown reason.");
                MenuManager.Instance.PromptMenu.ShowPromptMenu(
                    PromptMenuCanvas.DefaultErrorPromptMessage,
                    SessionEssentialsModels.FailedToFindServerMessage,
                    PromptMenuCanvas.DefaultOkMessage, null);
                break;
            default:
                BytewarsLogger.Log($"Received dedicated server status change. Status: {dsInfo.StatusV2}");
                break;
        }
    }

    private void UpdatePartyMemberGameSessionStatus()
    {
        if (!IsInParty)
        {
            return;
        }

        MenuCanvas menuCanvas = MenuManager.Instance.GetCurrentMenu();  
        bool isInMatchLobby = menuCanvas != null && menuCanvas is MatchLobbyMenu;
        bool isInGameplay = SceneManager.GetActiveScene().buildIndex is GameConstant.GameSceneBuildIndex;
        bool isInGameSession = isInMatchLobby || isInGameplay;

        // No need to update if the status is the same.
        if (isInGameSession == cachedInGameStatus)
        {
            return;
        }

        // Update party member game session status
        Session.GetPartyDetails(CachedParty.id, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Update party member game session status failed. Error {result.Error.Code}: {result.Error.Message}");
                return;
            }

            SessionV2PartySession partySession = result.Value;
            SessionV2PartySessionUpdateRequest request = new();
            request.version = partySession.version;
            request.attributes = partySession.attributes;

            // Parse old statuses.
            Dictionary<string /*userId*/, bool /*isInGameSession*/> memberGameSessionStatuses = new();
            if (request.attributes.TryGetValue(
                PlayingWithPartyModels.PartyMembersGameSessionStatusesKey, out object value))
            {
                memberGameSessionStatuses = JObject.FromObject(value).ToObject<Dictionary<string, bool>>();
            }

            // Update current player game session status.
            memberGameSessionStatuses[GameData.CachedPlayerState.PlayerId] = isInGameSession;
            request.attributes[PlayingWithPartyModels.PartyMembersGameSessionStatusesKey] = memberGameSessionStatuses;

            Session.PatchUpdateParty(partySession.id, request, (patchResult) =>
            {
                if (result.IsError)
                {
                    BytewarsLogger.LogWarning($"Update party member game session status failed. Error {patchResult.Error.Code}: {patchResult.Error.Message}");
                    return;
                }

                BytewarsLogger.Log($"Update party member game session status success. Current status: {isInGameSession}");
                cachedInGameStatus = isInGameSession;
            });
        });
    }

    private async UniTask<bool> IsValidToStartPartyGameSession()
    {
        // If not in party session, no need to validate.
        if (!IsInParty)
        {
            return true;
        }

        // Only party leader is able to start party game session.
        if (!IsPartyLeader)
        {
            MenuManager.Instance.PushNotification(new PushNotificationModel()
            {
                Message = PlayingWithPartyModels.PartyGameSessionMemberSafeguardMessage,
                UseDefaultIconOnEmpty = false
            });
            return false;
        }

        // Get party member game session status.
        UniTaskCompletionSource<bool> task = new();
        Session.GetPartyDetails(CachedParty.id, (result) =>
        {
            if (result.IsError)
            {
                task.TrySetResult(false);
                return;
            }

            // Parse party member game session status.
            Dictionary<string /*userId*/, bool /*isInGameSession*/> memberGameSessionStatuses = new();
            if (result.Value.attributes.TryGetValue(
                PlayingWithPartyModels.PartyMembersGameSessionStatusesKey, out object value))
            {
                memberGameSessionStatuses = JObject.FromObject(value).ToObject<Dictionary<string, bool>>();
            }

            // Only validate active members.
            string[] activeMembers = result.Value.members.
                Where(x => x.StatusV2 == SessionV2MemberStatus.JOINED).
                Select(x => x.id).ToArray();
            memberGameSessionStatuses = memberGameSessionStatuses
                .Where(x => activeMembers.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Check if all member are available for new game session.
            bool isAllMemberAvailable = memberGameSessionStatuses.All(x => x.Value == false);
            if (!isAllMemberAvailable)
            {
                MenuManager.Instance.PushNotification(new PushNotificationModel()
                {
                    Message = PlayingWithPartyModels.PartyGameSessionLeaderSafeguardMessage,
                    UseDefaultIconOnEmpty = false
                });
            }

            task.TrySetResult(isAllMemberAvailable);
        });

        return await task.Task;
    }

    private async UniTask<bool> IsValidToJoinPartyGameSession(SessionV2GameSession gameSession)
    {
        if (!IsInParty)
        {
            return true;
        }

        if (!await IsValidToStartPartyGameSession())
        {
            return false;
        }

        // Count active members with joined status
        int activeMemberCount = gameSession.members.Count(m => m.StatusV2 == SessionV2MemberStatus.JOINED);

        bool isAvailable = !gameSession.IsFull && gameSession?.configuration.maxPlayers - activeMemberCount >= CachedParty?.members.Length;

        // Notify that no more slots to join the session.
        if (!isAvailable)
        {
            MenuManager.Instance.PushNotification(new PushNotificationModel()
            {
                Message = PlayingWithPartyModels.JoinPartyGameSessionSafeguardMessage,
                UseDefaultIconOnEmpty = false
            });
        }

        return isAvailable;
    }

    private async UniTask<bool> IsValidToStartPartyMatchmaking(InGameMode gameMode)
    {
        if (!IsInParty)
        {
            return true;
        }

        if (!await IsValidToStartPartyGameSession())
        {
            return false;
        }

        // No need to validate if there is only one member in party.
        if (CachedParty?.members.Length <= 1)
        {
            return true;
        }

        // Check whether matchmaking with party is supported using the specified game mode.
        bool isSupported = gameMode is InGameMode.MatchmakingTeamDeathmatch or InGameMode.CreateMatchTeamDeathmatch;
        if (!isSupported)
        {
            MenuManager.Instance.PushNotification(new PushNotificationModel()
            {
                Message = PlayingWithPartyModels.PartyMatchmakingSafeguardMessage,
                UseDefaultIconOnEmpty = false
            });
        }

        return isSupported;
    }
}
