// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static SessionEssentialsModels;
using static MatchmakingEssentialsModels;

public class MatchmakingP2PMenu : MenuCanvas
{
    [SerializeField] private MatchmakingStateSwitcher stateSwitcher;

    private MatchmakingP2PWrapper matchmakingP2PWrapper;

    private void Awake()
    {
        stateSwitcher.OnRetryButtonClicked = StartMatchmaking;
    }

    private void OnEnable()
    {
        matchmakingP2PWrapper ??= TutorialModuleManager.Instance.GetModuleClass<MatchmakingP2PWrapper>();
        if (matchmakingP2PWrapper) StartMatchmaking();
    }

    private void OnDisable()
    {
        ClearMatchmakingEvents();
    }

    private void RegisterMatchmakingEvents()
    {
        if (!matchmakingP2PWrapper) return;
        MatchmakingEssentialsWrapper.OnMatchFound += OnMatchmakingComplete;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired += OnMatchmakingExpired;
        MatchmakingEssentialsWrapper.OnSessionInviteReceived += OnSessionInviteReceived;
    }

    private void ClearMatchmakingEvents()
    {
        if (!matchmakingP2PWrapper) return;
        MatchmakingEssentialsWrapper.OnMatchFound -= OnMatchmakingComplete;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired -= OnMatchmakingExpired;
        MatchmakingEssentialsWrapper.OnSessionInviteReceived -= OnSessionInviteReceived;
    }

    private void StartMatchmaking()
    {
        // Get the match pool name from the game config.
        SessionV2GameSessionCreateRequest request =
            AccelByteWarsOnlineSessionModels.GetGameSessionRequestModel(MatchmakingMenu.SelectedGameMode, GameSessionServerType.PeerToPeer);
        if (request == null)
        {
            BytewarsLogger.LogWarning("Failed to start matchmaking. Session type is not supported.");
            stateSwitcher.ErrorMessage = InvalidSessionTypeMessage;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        // Reregister matchmaking events.
        ClearMatchmakingEvents();
        RegisterMatchmakingEvents();

        // Start matchmaking
        stateSwitcher.SetState(MatchmakingMenuState.StartMatchmaking);
        matchmakingP2PWrapper.StartMatchmaking(request.matchPool, OnStartMatchmakingComplete);
    }

    private void CancelMatchmaking(string matchTicketId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.CancelMatchmaking);
        matchmakingP2PWrapper.CancelMatchmaking(matchTicketId, OnCancelMatchmakingComplete);
    }

    private void JoinSession(string sessionId)
    {
        stateSwitcher.OnCancelButtonClicked = () => LeaveSession(sessionId);
        stateSwitcher.SetState(MatchmakingMenuState.JoinMatch);
        matchmakingP2PWrapper.JoinGameSession(sessionId, OnJoinSessionComplete);
    }

    private void LeaveSession(string sessionId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.LeaveMatch);
        matchmakingP2PWrapper.LeaveGameSession(sessionId, OnLeaveSessionComplete);
    }

    private void RejectSessionInvite(string sessionId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.RejectMatch);
        matchmakingP2PWrapper.RejectGameSessionInvite(sessionId, OnRejectSessionInviteComplete);
    }

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (result.IsError)
        {
            ClearMatchmakingEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        stateSwitcher.OnCancelButtonClicked = () => CancelMatchmaking(result.Value.matchTicketId);
        stateSwitcher.SetState(MatchmakingMenuState.FindingMatch);
    }

    private void OnCancelMatchmakingComplete(Result result)
    {
        if (result.IsError)
        {
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        MenuManager.Instance.OnBackPressed();
    }

    private void OnMatchmakingComplete(Result<MatchmakingV2MatchFoundNotification> result)
    {
        if (result.IsError)
        {
            ClearMatchmakingEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        MatchmakingEssentialsWrapper.OnMatchFound -= OnMatchmakingComplete;

        // Only change the menu state if the session invite is not already received.
        if (stateSwitcher.CurrentState != MatchmakingMenuState.JoinMatchConfirmation)
        {
            stateSwitcher.SetState(MatchmakingMenuState.MatchFound);
        }
    }

    private void OnMatchmakingExpired(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        ClearMatchmakingEvents();
        stateSwitcher.ErrorMessage = result.IsError ? result.Error.Message : MatchmakingExpiredMessage;
        stateSwitcher.SetState(MatchmakingMenuState.Error);
    }

    private void OnJoinSessionComplete(Result<SessionV2GameSession> result)
    {
        // Abort if already attempt to cancel join the match.
        if (stateSwitcher.CurrentState != MatchmakingMenuState.JoinMatch)
        {
            return;
        }

        if (result.IsError)
        {
            ClearMatchmakingEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        stateSwitcher.SetState(MatchmakingMenuState.JoinedMatch);
    }

    private void OnLeaveSessionComplete(Result result)
    {
        if (result.IsError)
        {
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        MenuManager.Instance.OnBackPressed();
    }

    private void OnRejectSessionInviteComplete(Result result)
    {
        if (result.IsError)
        {
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        MenuManager.Instance.OnBackPressed();
    }

    private async void OnSessionInviteReceived(Result<SessionV2GameInvitationNotification> result)
    {
        if (result.IsError)
        {
            ClearMatchmakingEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        MatchmakingEssentialsWrapper.OnSessionInviteReceived -= OnSessionInviteReceived;

        stateSwitcher.OnJoinButtonClicked = () => JoinSession(result.Value.sessionId);
        stateSwitcher.OnRejectButtonClicked = () => RejectSessionInvite(result.Value.sessionId);

        /* Delay a bit to prevent menu state overlap.
         * Since the session invite and match found event may be received at the same time. */
        await UniTask.Delay(StateChangeDelay * 1000);

        stateSwitcher.SetState(MatchmakingMenuState.JoinMatchConfirmation);
    }

    public override GameObject GetFirstButton()
    {
        return stateSwitcher.DesiredFocus;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingP2PMenu;
    }
}
