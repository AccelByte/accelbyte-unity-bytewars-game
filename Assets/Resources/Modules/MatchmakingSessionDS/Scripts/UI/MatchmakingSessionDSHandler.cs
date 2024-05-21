// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionDSHandler : MenuCanvas
{
    private const string eliminationWithDSMatchPoolName = "unity-elimination-ds";
    private const string teamdeathmatchWithDSMatchPoolName = "unity-teamdeathmatch-ds";
    private const string eliminationDSAMSMatchPool = "unity-elimination-ds-ams";
    private const string teamDeathmatchDSAMSMatchPool = "unity-teamdeathmatch-ds-ams";
    private InGameMode selectedGameMode = InGameMode.None;
    private MatchmakingSessionDSWrapper matchmakingSessionDSWrapper;
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;

    public void ClickDedicatedServerButton()
    {
        var menu = MenuManager.Instance.GetCurrentMenu();
        if (menu is MatchmakingSessionServerTypeSelection serverTypeSelection)
        {
            InitWrapper();
            selectedGameMode = serverTypeSelection.SelectedGameMode;
        }
        else
        {
            BytewarsLogger.LogWarning("Current menu is not server type selection menu while try to matchmaking with DS");
            return;
        }
        switch (selectedGameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode:
                string teamdeathMatchPoolName = teamdeathmatchWithDSMatchPoolName;
                if (GConfig.IsUsingAMS())
                {
                    teamdeathMatchPoolName = teamDeathmatchDSAMSMatchPool;
                }
                matchmakingSessionDSWrapper.StartDSMatchmaking(teamdeathMatchPoolName);
                ShowLoading("Finding Team Death Match (Dedicated Server)...",
                "Finding Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
            case InGameMode.OnlineEliminationGameMode:
                string eliminationMatchPoolName = eliminationWithDSMatchPoolName;
                if (GConfig.IsUsingAMS())
                {
                    eliminationMatchPoolName = eliminationDSAMSMatchPool;
                }
                matchmakingSessionDSWrapper.StartDSMatchmaking(eliminationMatchPoolName);
                ShowLoading("Finding Elimination Match (Dedicated Server)...",
                "Finding Elimination Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
            default:
                string errorMsg = $"No Dedicated Server Match Pool for {selectedGameMode}";
                BytewarsLogger.LogWarning(errorMsg);
                ShowError(errorMsg);
                break;
        }
    }

    private void InitWrapper()
    {
        if (matchmakingSessionDSWrapper == null)
        {
            matchmakingSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();
        }

        BindMatchmakingEvent();

        MatchmakingSessionServerTypeSelection.OnBackButtonCalled -= OnBackButtonFromServerSelection;
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled += OnBackButtonFromServerSelection;
    }

    private void OnBackButtonFromServerSelection()
    {
        UnbindMatchmakingEvents();
    }

    private void BindMatchmakingEvent()
    {
        if (matchmakingSessionDSWrapper == null)
        {
            return;
        }

        matchmakingSessionDSWrapper.BindEventListener();

        // listen event when match is found and ds available
        matchmakingSessionDSWrapper.OnMatchmakingFoundEvent += JoinSessionPanel;
        matchmakingSessionDSWrapper.OnJoinSessionCompleteEvent += WaitingForDSPanel;
        matchmakingSessionDSWrapper.OnDSAvailableEvent += TravelToDS;

        // listen event when failed
        matchmakingSessionDSWrapper.OnStartMatchmakingFailed += FailedPanel;
        matchmakingSessionDSWrapper.OnMatchmakingJoinSessionFailedEvent += FailedPanel;
        matchmakingSessionDSWrapper.OnDSFailedRequestEvent += FailedPanel;
        matchmakingSessionDSWrapper.OnSessionEnded += FailedPanel;
        matchmakingSessionDSWrapper.OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    private void UnbindMatchmakingEvents()
    {
        //remove all events
        if (matchmakingSessionDSWrapper == null)
        {
            return;
        }
        matchmakingSessionDSWrapper?.UnbindEventListener();
        
        matchmakingSessionDSWrapper.OnMatchmakingFoundEvent -= JoinSessionPanel;
        matchmakingSessionDSWrapper.OnJoinSessionCompleteEvent -= WaitingForDSPanel;
        matchmakingSessionDSWrapper.OnDSAvailableEvent -= TravelToDS;
        matchmakingSessionDSWrapper.OnStartMatchmakingFailed -= FailedPanel;
        matchmakingSessionDSWrapper.OnMatchmakingJoinSessionFailedEvent -= FailedPanel;
        matchmakingSessionDSWrapper.OnDSFailedRequestEvent -= FailedPanel;
        matchmakingSessionDSWrapper.OnSessionEnded -= FailedPanel;
        matchmakingSessionDSWrapper.OnCancelMatchmakingCompleteEvent -= OnCancelMatchmakingComplete;
    }

    private void FailedPanel()
    {
        UnbindMatchmakingEvents();
        ShowError("Cannot find Match");
    }

    private void OnCancelMatchmakingComplete()
    {
        UnbindMatchmakingEvents();
        HideLoading();
    }

    private void TravelToDS(SessionV2GameSession session)
    {
        UnbindMatchmakingEvents();
        matchmakingSessionDSWrapper.UnbindEventListener();
        matchmakingSessionDSWrapper.TravelToDS(session, selectedGameMode);
    }

    private void JoinSessionPanel(string sessionId)
    {
        ShowLoading("Joining Match", "Joining Match is timed out", joinSessionTimeoutSec, ClientLeaveGameSession);
    }

    private void WaitingForDSPanel(SessionResponsePayload payload)
    {
        if (payload.TutorialType != TutorialType.MatchmakingWithDS)
        {
            return;
        }
        ShowLoading("Waiting For DS", "DS Waiting is timed out", joinSessionTimeoutSec, ClientLeaveGameSession);
    }

    private void CancelDSMatchmaking()
    {
        matchmakingSessionDSWrapper.CancelDSMatchmaking();
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void ClientLeaveGameSession()
    {
        UnbindMatchmakingEvents();

        HideLoading();
        matchmakingSessionDSWrapper.OnClientLeave();
    }

    #region MenuCanvas
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingSessionDS;
    }
    #endregion MenuCanvas
}
