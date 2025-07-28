// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
using static SessionEssentialsModels;
using static MatchSessionEssentialsModels;

public class CreateMatchSessionDSMenu : MenuCanvas
{
    [SerializeField] private MatchSessionStateSwitcher stateSwitcher;

    private BrowseSessionModel sessionToJoin = null;

    private MatchSessionDSWrapper matchSessionDSWrapper;

    private void Awake()
    {
        BrowseMatchEntry.OnJoinDSMatchButtonClicked += (BrowseSessionModel sessionModel) =>
        {
            sessionToJoin = sessionModel;
            MenuManager.Instance.ChangeToMenu(GetAssetEnum());
        };
    }

    private void OnEnable()
    {
        matchSessionDSWrapper ??= TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
        if (!matchSessionDSWrapper)
        {
            return;
        }

        if (sessionToJoin == null)
        {
            stateSwitcher.OnRetryButtonClicked = CreateMatchSession;
            stateSwitcher.OnCancelButtonClicked = () => LeaveMatchSession(AccelByteWarsOnlineSession.CachedSession.id);
            CreateMatchSession();
        }
        else
        {
            stateSwitcher.OnRetryButtonClicked = () => JoinMatchSession(sessionToJoin.Session.id);
            stateSwitcher.OnCancelButtonClicked = () => LeaveMatchSession(sessionToJoin.Session.id);
            JoinMatchSession(sessionToJoin.Session.id);
        }
    }

    private void OnDisable()
    {
        sessionToJoin = null;
        ClearMatchSessionEvents();
    }

    private void RegisterMatchSessionEvents()
    {
        if (!matchSessionDSWrapper) return;
        MatchSessionEssentialsWrapper.OnDSStatusChanged += OnDSStatusChanged;
    }

    private void ClearMatchSessionEvents()
    {
        if (!matchSessionDSWrapper) return;
        MatchSessionEssentialsWrapper.OnDSStatusChanged -= OnDSStatusChanged;
    }

    private void CreateMatchSession()
    {
        // Get the session template name from the game config.
        SessionV2GameSessionCreateRequest request =
            AccelByteWarsOnlineSessionModels.GetGameSessionRequestModel(CreateMatchSessionMenu.SelectedGameMode, GameSessionServerType.DedicatedServerAMS);
        if (request == null)
        {
            BytewarsLogger.LogWarning("Failed to create match session. Session type is not supported.");
            stateSwitcher.ErrorMessage = InvalidSessionTypeMessage;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        // Reregister match session events.
        ClearMatchSessionEvents();
        RegisterMatchSessionEvents();

        // Create a new match session.
        stateSwitcher.SetState(MatchSessionMenuState.CreateMatch);
        matchSessionDSWrapper.CreateGameSession(request, OnJoinSessionComplete);
    }

    private void JoinMatchSession(string sessionId)
    {
        // Reregister match session events.
        ClearMatchSessionEvents();
        RegisterMatchSessionEvents();

        stateSwitcher.SetState(MatchSessionMenuState.JoinMatch);
        matchSessionDSWrapper.JoinGameSession(sessionId, OnJoinSessionComplete);
    }

    private void LeaveMatchSession(string sessionId)
    {
        ClearMatchSessionEvents();

        // Simply return from the menu if the session is empty.
        if (string.IsNullOrEmpty(sessionId))
        {
            MenuManager.Instance.OnBackPressed();
            return;
        }

        stateSwitcher.SetState(MatchSessionMenuState.LeaveMatch);
        matchSessionDSWrapper.LeaveGameSession(sessionId, OnLeaveMatchSessionComplete);
    }

    private async void OnJoinSessionComplete(Result<SessionV2GameSession> result)
    {
        // Abort if already attempt to cancel join the match.
        if (stateSwitcher.CurrentState is
            not (MatchSessionMenuState.CreateMatch or MatchSessionMenuState.JoinMatch))
        {
            return;
        }

        if (result.IsError)
        {
            ClearMatchSessionEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        stateSwitcher.SetState(MatchSessionMenuState.JoinedMatch);
        await UniTask.Delay(StateChangeDelay * 1000); // Delay a bit before changing menu state.
        stateSwitcher.SetState(MatchSessionMenuState.RequestingServer);
    }

    private void OnLeaveMatchSessionComplete(Result result)
    {
        if (result.IsError && result.Error.Code != ErrorCode.SessionIdNotFound)
        {
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        MenuManager.Instance.OnBackPressed();
    }

    private void OnDSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (result.IsError)
        {
            ClearMatchSessionEvents();
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        SessionV2DsInformation dsInfo = result.Value.session.dsInformation;
        switch (dsInfo.StatusV2)
        {
            case SessionV2DsStatus.AVAILABLE:
                ClearMatchSessionEvents();
                break;
            case SessionV2DsStatus.FAILED_TO_REQUEST:
            case SessionV2DsStatus.ENDED:
            case SessionV2DsStatus.UNKNOWN:
                ClearMatchSessionEvents();
                stateSwitcher.ErrorMessage = FailedToFindServerMessage;
                stateSwitcher.SetState(MatchSessionMenuState.Error);
                break;
            default:
                stateSwitcher.SetState(MatchSessionMenuState.RequestingServer);
                break;
        }
    }

    public override GameObject GetFirstButton()
    {
        return stateSwitcher.DesiredFocus;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionDSMenu;
    }
}
