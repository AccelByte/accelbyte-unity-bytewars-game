// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static SessionEssentialsModels;
using static MatchmakingEssentialsModels;

public class MatchmakingDSMenu : MenuCanvas
{
    [SerializeField] private MatchmakingStateSwitcher stateSwitcher;

    private MatchmakingDSWrapper matchmakingDSWrapper;

    private void Awake()
    {
        stateSwitcher.OnRetryButtonClicked = StartMatchmaking;
    }

    private void OnEnable()
    {
        matchmakingDSWrapper ??= TutorialModuleManager.Instance.GetModuleClass<MatchmakingDSWrapper>();
        if (matchmakingDSWrapper) StartMatchmaking();
    }

    private void OnDisable()
    {
        ClearMatchmakingEvents();
    }

    private void RegisterMatchmakingEvents()
    {
        if (!matchmakingDSWrapper) return;
        MatchmakingEssentialsWrapper.OnMatchFound += OnMatchmakingComplete;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired += OnMatchmakingExpired;
        MatchmakingEssentialsWrapper.OnDSStatusChanged += OnDSStatusChanged;
        MatchmakingEssentialsWrapper.OnSessionInviteReceived += OnSessionInviteReceived;
    }

    private void ClearMatchmakingEvents()
    {
        if (!matchmakingDSWrapper) return;
        MatchmakingEssentialsWrapper.OnMatchFound -= OnMatchmakingComplete;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired -= OnMatchmakingExpired;
        MatchmakingEssentialsWrapper.OnDSStatusChanged -= OnDSStatusChanged;
        MatchmakingEssentialsWrapper.OnSessionInviteReceived -= OnSessionInviteReceived;
    }

    private void StartMatchmaking()
    {
        // Get the match pool name from the game config.
        SessionV2GameSessionCreateRequest request = 
            AccelByteWarsOnlineSessionModels.GetGameSessionRequestModel(MatchmakingMenu.SelectedGameMode, GameSessionServerType.DedicatedServerAMS);
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
        matchmakingDSWrapper.StartMatchmaking(request.matchPool, OnStartMatchmakingComplete);
    }

    private void CancelMatchmaking(string matchTicketId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.CancelMatchmaking);
        matchmakingDSWrapper.CancelMatchmaking(matchTicketId, OnCancelMatchmakingComplete);
    }

    private void JoinSession(string sessionId)
    {
        stateSwitcher.OnCancelButtonClicked = () => LeaveSession(sessionId);
        stateSwitcher.SetState(MatchmakingMenuState.JoinMatch);
        matchmakingDSWrapper.JoinGameSession(sessionId, OnJoinSessionComplete);
    }

    private void LeaveSession(string sessionId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.LeaveMatch);
        matchmakingDSWrapper.LeaveGameSession(sessionId, OnLeaveSessionComplete);
    }

    private void RejectSessionInvite(string sessionId)
    {
        ClearMatchmakingEvents();
        stateSwitcher.SetState(MatchmakingMenuState.RejectMatch);
        matchmakingDSWrapper.RejectGameSessionInvite(sessionId, OnRejectSessionInviteComplete);
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

    private async void OnJoinSessionComplete(Result<SessionV2GameSession> result)
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
        await UniTask.Delay(StateChangeDelay * 1000); // Delay a bit before changing menu state.
        stateSwitcher.SetState(MatchmakingMenuState.RequestingServer);
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

    private void OnDSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            ClearMatchmakingEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchmakingMenuState.Error);
            return;
        }

        SessionV2DsInformation dsInfo = result.Value.session.dsInformation;
        switch (dsInfo.StatusV2)
        {
            case SessionV2DsStatus.AVAILABLE:
                ClearMatchmakingEvents();
                break;
            case SessionV2DsStatus.FAILED_TO_REQUEST:
            case SessionV2DsStatus.ENDED:
            case SessionV2DsStatus.UNKNOWN:
                ClearMatchmakingEvents();
                stateSwitcher.ErrorMessage = FailedToFindServerMessage;
                stateSwitcher.SetState(MatchmakingMenuState.Error);
                break;
            default:
                stateSwitcher.SetState(MatchmakingMenuState.RequestingServer);
                break;
        }
    }

    public override GameObject GetFirstButton()
    {
        return stateSwitcher.DesiredFocus;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingDSMenu;
    }
}
