// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class MatchmakingSessionDSHandler : MenuCanvas
{
    private InGameMode selectedGameMode = InGameMode.None;
    private MatchmakingSessionDSWrapper matchmakingSessionDSWrapper;
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;

    public void ClickDedicatedServerButton()
    {
        MenuCanvas menu = MenuManager.Instance.GetCurrentMenu();
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
                matchmakingSessionDSWrapper.StartDSMatchmaking(InGameMode.OnlineDeathMatchGameMode);
                ShowLoading("Start Team Death Match (Dedicated Server)...",
                "Start Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec);
                break;
            case InGameMode.OnlineEliminationGameMode:
                matchmakingSessionDSWrapper.StartDSMatchmaking(InGameMode.OnlineEliminationGameMode);
                ShowLoading("Start Elimination Match (Dedicated Server)...",
                "Start Elimination Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec);
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

        matchmakingSessionDSWrapper.BindMatchmakingEvent();
        matchmakingSessionDSWrapper.OnMatchTicketDSCreated += OnMatchTicketDSCreated;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSMatchFound += OnMatchmakingWithDSMatchFound;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSTicketExpired += OnMatchmakingWithDSTicketExpired;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSJoinSessionStarted += OnMatchmakingWithDSJoinSessionStarted;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSJoinSessionCompleted += OnMatchmakingWithDSJoinSessionCompleted;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSCanceled += OnMatchmakingWithDSCanceled;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSError += ErrorPanel;
        matchmakingSessionDSWrapper.OnMatchmakingError += ErrorPanel;
        matchmakingSessionDSWrapper.OnDSError += ErrorPanel;
        matchmakingSessionDSWrapper.OnIntentionallyLeaveSession += Reset;
        matchmakingSessionDSWrapper.OnDSAvailable += Travelling;
        matchmakingSessionDSWrapper.OnInvitedToSession += OnInvitedToGameSession;
        matchmakingSessionDSWrapper.OnRejectGameSessionCompleteEvent += OnSessionRejected;
    }

    private void OnMatchTicketDSCreated()
    {
        switch (selectedGameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode:
                ShowLoading("Finding Team Death Match (Dedicated Server)...",
                "Finding Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
            case InGameMode.OnlineEliminationGameMode:
                ShowLoading("Finding Elimination Match (Dedicated Server)...",
                "Elimination Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
        }
    }

    private void UnbindMatchmakingEvents()
    {
        if (matchmakingSessionDSWrapper == null)
        {
            return;
        }

        matchmakingSessionDSWrapper.UnbindMatchmakingEvent();
        matchmakingSessionDSWrapper.OnMatchTicketDSCreated -= OnMatchTicketDSCreated;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSMatchFound -= OnMatchmakingWithDSMatchFound;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSTicketExpired -= OnMatchmakingWithDSTicketExpired;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSJoinSessionStarted -= OnMatchmakingWithDSJoinSessionStarted;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSJoinSessionCompleted -= OnMatchmakingWithDSJoinSessionCompleted;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSCanceled -= OnMatchmakingWithDSCanceled;
        matchmakingSessionDSWrapper.OnMatchmakingWithDSError -= ErrorPanel;
        matchmakingSessionDSWrapper.OnMatchmakingError -= ErrorPanel;
        matchmakingSessionDSWrapper.OnDSError -= ErrorPanel;
        matchmakingSessionDSWrapper.OnIntentionallyLeaveSession -= Reset;
        matchmakingSessionDSWrapper.OnDSAvailable -= Travelling;
        matchmakingSessionDSWrapper.OnInvitedToSession -= OnInvitedToGameSession;
        matchmakingSessionDSWrapper.OnRejectGameSessionCompleteEvent -= OnSessionRejected;
    }

    private void OnSessionRejected(bool successed)
    {
        if (!successed)
        {
            CancelDSMatchmaking();
        }

        ShowAdditionalInfo("Match Rejected");
        ShowInfo("Match is rejected");
        Reset();
    }

    private void OnInvitedToGameSession()
    {
        ShowLoading("Match Is Ready", "Cancelling match is timed out", 
            cancelMatchmakingTimeoutSec, RejectMatch,
            okCallback:AcceptMatch, okButtonText: "Accept", cancelButtonText: "Reject" );
    }

    private void OnMatchmakingWithDSJoinSessionStarted()
    {
        ShowLoading("Joining Match", "Joining Match is timed out", joinSessionTimeoutSec);
    }

    private void OnMatchmakingWithDSJoinSessionCompleted()
    {
        ShowLoading("Requesting Server", "Requesting Server timed out", joinSessionTimeoutSec, OnDSTimeOut, false);
    }

    private void Travelling(bool isAvailable)
    {
        ShowAdditionalInfo("Server Found");
        Reset();
    }

    private void OnMatchmakingWithDSTicketExpired()
    {
        ShowError("Matchmaking ticket is expired");
        Reset();
    }

    private void OnMatchmakingWithDSMatchFound()
    {
        ShowAdditionalInfo("Match Found", hideButton: true);
    }

    private void ErrorPanel(string message)
    {
        ShowError(message);
        Reset();
    }

    private void OnMatchmakingWithDSCanceled()
    {
        ShowInfo("Matchmaking is Canceled");
        Reset();
    }

    private void Reset()
    {
        selectedGameMode = InGameMode.None;
        UnbindMatchmakingEvents();
    }

    private void OnDSTimeOut()
    {
        matchmakingSessionDSWrapper.LeaveCurrentSession();
        Reset();
    }

    private void CancelDSMatchmaking()
    {
        matchmakingSessionDSWrapper.CancelDSMatchmaking();
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void RejectMatch()
    {
        matchmakingSessionDSWrapper.RejectSessionInvitation();
        ShowLoading("Rejecting Match", "Rejecting Match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void AcceptMatch()
    {
        matchmakingSessionDSWrapper.AcceptSessionInvitation();
        ShowLoading("Joining Match", "Rejecting Match is timed out", cancelMatchmakingTimeoutSec);
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
