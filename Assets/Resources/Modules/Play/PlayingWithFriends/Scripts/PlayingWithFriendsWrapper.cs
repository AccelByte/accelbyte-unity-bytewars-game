// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;

public class PlayingWithFriendsWrapper : SessionEssentialsWrapper
{
    private void OnEnable()
    {
        Lobby.SessionV2InvitedUserToGameSession += OnGameSessionInviteReceived;
        Lobby.SessionV2UserRejectedGameSessionInvitation += OnGameSessionInviteRejected;
    }

    private void OnDisable()
    {
        Lobby.SessionV2InvitedUserToGameSession -= OnGameSessionInviteReceived;
        Lobby.SessionV2UserRejectedGameSessionInvitation -= OnGameSessionInviteRejected;
    }

    public void SendInviteToCurrentGameSession(string userId, ResultCallback onComplete)
    {
        SendGameSessionInvite(CachedSession.id, userId, (Result result) =>
        {
            // Display notification.
            MenuManager.Instance.PushNotification(new PushNotificationModel
            {
                Message = result.IsError ? PlayingWithFriendsModels.SendGameSessionInviteError : PlayingWithFriendsModels.SendGameSessionInviteSuccess,
                UseDefaultIconOnEmpty = true
            });
            onComplete?.Invoke(result);
        });
    }

    private void OnGameSessionInviteReceived(Result<SessionV2GameInvitationNotification> notif)
    {
        if (notif.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle received game session invitation. Error {notif.Error.Code}: {notif.Error.Message}");
            return;
        }
        
        // Construct local function to display push notification.
        void OnGetSenderInfoCompleted(Result<AccountUserPlatformInfosResponse> result)
        {
            AccountUserPlatformData senderInfo = result.IsError ? null : result.Value.Data[0];
            if (senderInfo == null)
            {
                BytewarsLogger.LogWarning($"Failed to get sender info. Error {result.Error.Code}: {result.Error.Message}");

                return;
            }

            MenuManager.Instance.PushNotification(new PushNotificationModel
            {
                Message = senderInfo.DisplayName + PlayingWithFriendsModels.InviteReceived,
                IconUrl = senderInfo.AvatarUrl,
                UseDefaultIconOnEmpty = true,
                ActionButtonTexts = new string[]
                {
                    PlayingWithFriendsModels.InviteAccept,
                    PlayingWithFriendsModels.InviteReject
                },
                ActionButtonCallback = (PushNotificationActionResult actionResult) =>
                {
                    switch (actionResult)
                    {
                        // Show accept party invitation confirmation.
                        case PushNotificationActionResult.Button1:
                            JoinGameSession(notif.Value.sessionId, (Result<SessionV2GameSession> result) =>
                            {
                                OnJoinGameSessionCompleted(result, null);
                            });
                            break;

                        // Reject party invitation.
                        case PushNotificationActionResult.Button2:
                            RejectGameSessionInvite(notif.Value.sessionId, null);
                            break;
                    }
                }
            });
        }

        // Construct local function to get sender info.
        void OnGetGameSessionDetailsCompleted(Result<SessionV2GameSession> result)
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to get game session details. Error {result.Error.Code}: {result.Error.Message}");

                return;
            }

            User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { result.Value.leaderId }, OnGetSenderInfoCompleted);
        }

        // Get session info.
        Session.GetGameSessionDetailsBySessionId(notif.Value.sessionId, OnGetGameSessionDetailsCompleted);
    }

    private void OnJoinGameSessionCompleted(Result<SessionV2GameSession> result, ResultCallback<SessionV2GameSession> onComplete = null)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to received game session user joined notification. Error {result.Error.Code}: {result.Error.Message}");
            onComplete?.Invoke(result);
            return;
        }

        // Update cached session ID.
        GameData.ServerSessionID = result.Value.id;
        
        SessionV2GameSession gameSession = result.Value;
        switch (gameSession.configuration.type)
        {
            case SessionConfigurationTemplateType.DS:
                if (
                    gameSession.dsInformation == null ||
                    gameSession.dsInformation.status != SessionV2DsStatus.AVAILABLE
                )
                {
                    // Server is not ready, listen to DS event.
                    Lobby.SessionV2DsStatusChanged += OnDsStatusChanged;
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
                break;
        }
        
        onComplete?.Invoke(result);
    }

    private void OnGameSessionInviteRejected(Result<SessionV2GameInvitationRejectedNotification> notif)
    {
        if (notif.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to received game session user joined notification. Error {notif.Error.Code}: {notif.Error.Message}");
            return;
        }
        
        // Construct local function to display push notification.
        void OnGetSenderInfoCompleted(Result<AccountUserPlatformInfosResponse> result)
        {
            AccountUserPlatformData receiverInfo = result.IsError ? null : result.Value.Data[0];
            if (receiverInfo == null)
            {
                BytewarsLogger.LogWarning($"Failed to get sender info. Error {result.Error.Code}: {result.Error.Message}");
                return;
            }

            MenuManager.Instance.PushNotification(new PushNotificationModel
            {
                Message = receiverInfo.UniqueDisplayName + PlayingWithFriendsModels.InviteRejected,
                IconUrl = receiverInfo.AvatarUrl,
                UseDefaultIconOnEmpty = true
            });
        }

        // Construct local function to get sender info.
        User.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", new string[] { notif.Value.rejectedId }, OnGetSenderInfoCompleted);
    }

    private void OnDsStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        Lobby.SessionV2DsStatusChanged -= OnDsStatusChanged;
        
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Dedicated server information received with error. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }
        if (
            result.Value.session.dsInformation == null ||
            result.Value.session.dsInformation.status != SessionV2DsStatus.AVAILABLE
        )
        {
            TravelToDS(result.Value.session, AccelByteWarsOnlineSessionModels.GetGameSessionGameMode(result.Value.session));
        }
        else
        {
            BytewarsLogger.LogWarning("Failed to travel to dedicated server. Dedicated server information not found.");
        }
    }
}
