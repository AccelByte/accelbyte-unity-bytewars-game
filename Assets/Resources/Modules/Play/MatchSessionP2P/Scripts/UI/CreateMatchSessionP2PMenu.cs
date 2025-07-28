// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using static SessionEssentialsModels;
using static MatchSessionEssentialsModels;

public class CreateMatchSessionP2PMenu : MenuCanvas
{
    [SerializeField] private MatchSessionStateSwitcher stateSwitcher;

    private BrowseSessionModel sessionToJoin = null;

    private MatchSessionP2PWrapper matchSessionP2PWrapper;

    private void Awake()
    {
        BrowseMatchEntry.OnJoinP2PMatchButtonClicked += (BrowseSessionModel sessionModel) =>
        {
            sessionToJoin = sessionModel;
            MenuManager.Instance.ChangeToMenu(GetAssetEnum());
        };
    }

    private void OnEnable()
    {
        matchSessionP2PWrapper ??= TutorialModuleManager.Instance.GetModuleClass<MatchSessionP2PWrapper>();
        if (!matchSessionP2PWrapper)
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
    }

    private void CreateMatchSession()
    {
        // Get the session template name from the game config.
        SessionV2GameSessionCreateRequest request =
            AccelByteWarsOnlineSessionModels.GetGameSessionRequestModel(CreateMatchSessionMenu.SelectedGameMode, GameSessionServerType.PeerToPeer);
        if (request == null)
        {
            BytewarsLogger.LogWarning("Failed to create match session. Session type is not supported.");
            stateSwitcher.ErrorMessage = InvalidSessionTypeMessage;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        // Create a new match session.
        stateSwitcher.SetState(MatchSessionMenuState.CreateMatch);
        matchSessionP2PWrapper.CreateGameSession(request, OnJoinSessionComplete);
    }

    private void JoinMatchSession(string sessionId)
    {
        stateSwitcher.SetState(MatchSessionMenuState.JoinMatch);
        matchSessionP2PWrapper.JoinGameSession(sessionId, OnJoinSessionComplete);
    }

    private void LeaveMatchSession(string sessionId)
    {
        // Simply return from the menu if the session is empty.
        if (string.IsNullOrEmpty(sessionId))
        {
            MenuManager.Instance.OnBackPressed();
            return;
        }

        stateSwitcher.SetState(MatchSessionMenuState.LeaveMatch);
        matchSessionP2PWrapper.LeaveGameSession(sessionId, OnLeaveMatchSessionComplete);
    }

    private void OnJoinSessionComplete(Result<SessionV2GameSession> result)
    {
        // Abort if already attempt to cancel join the match.
        if (stateSwitcher.CurrentState is 
            not (MatchSessionMenuState.CreateMatch or MatchSessionMenuState.JoinMatch))
        {
            return;
        }

        if (result.IsError)
        {
            stateSwitcher.ErrorMessage = result.Error.Message;
            stateSwitcher.SetState(MatchSessionMenuState.Error);
            return;
        }

        stateSwitcher.SetState(MatchSessionMenuState.JoinedMatch);
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

    public override GameObject GetFirstButton()
    {
        return stateSwitcher.DesiredFocus;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionP2PMenu;
    }
}
