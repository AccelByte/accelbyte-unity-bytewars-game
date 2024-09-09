// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionP2PHandler : MenuCanvas
{
    private MatchmakingSessionP2PWrapper matchmakingP2PWrapper;
    private InGameMode selectedGameMode = InGameMode.None;
    private const string eliminationInfo = "Elimination Match (Peer To Peer)";
    private const string teamdeathmatchInfo = "Team Death Match (Peer To Peer)";
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;

    public void ClickPeerToPeerButton()
    {
        MenuCanvas menu = MenuManager.Instance.GetCurrentMenu();
        if (menu is MatchmakingSessionServerTypeSelection serverTypeSelection)
        {
            InitWrapper();
            selectedGameMode = serverTypeSelection.SelectedGameMode;
        }
        else
        {
            BytewarsLogger.LogWarning("Current menu is not server type selection menu while try to matchmaking with Peer to Peer");
        }
        switch (selectedGameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode:
                matchmakingP2PWrapper.StartP2PMatchmaking(InGameMode.OnlineDeathMatchGameMode);
                ShowLoading($"Start {teamdeathmatchInfo}...",
                    $"Start {teamdeathmatchInfo} is timed out",
                    matchmakingTimeoutSec);
                break;
            case InGameMode.OnlineEliminationGameMode:
                matchmakingP2PWrapper.StartP2PMatchmaking(InGameMode.OnlineEliminationGameMode);
                ShowLoading($"Start {eliminationInfo}...",
                    $"Start {eliminationInfo} is timed out",
                    matchmakingTimeoutSec);
                break;
            default:
                string errorMsg = $"No Peer To Peer MatchPoolName for {selectedGameMode}";
                BytewarsLogger.LogWarning(errorMsg);
                ShowError(errorMsg);
                break;
        }
    }

    private void InitWrapper()
    {
        if (matchmakingP2PWrapper == null)
        {
            matchmakingP2PWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionP2PWrapper>();
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
        if (matchmakingP2PWrapper == null)
        {
            return;
        }
        
        matchmakingP2PWrapper.BindMatchmakingEvent();
        matchmakingP2PWrapper.OnMatchTicketP2PCreated += OnMatchTicketP2PCreated;
        matchmakingP2PWrapper.OnMatchmakingWithP2PMatchFound += OnMatchFound;
        matchmakingP2PWrapper.OnMatchmakingWithP2PTicketExpired += OnMatchmakingWithP2PTicketExpired;
        matchmakingP2PWrapper.OnMatchmakingWithP2PJoinSessionStarted += OnMatchmakingWithP2PJoinSessionStarted;
        matchmakingP2PWrapper.OnMatchmakingWithP2PJoinSessionCompleted += OnMatchmakingWithP2PJoinSessionCompleted;
        matchmakingP2PWrapper.OnMatchmakingWithP2PCanceled += OnMatchmakingWithP2PCanceledAsync;
        matchmakingP2PWrapper.OnMatchmakingWithP2PError += ErrorPanelAsync;
        matchmakingP2PWrapper.OnMatchmakingError += ErrorPanelAsync;
        matchmakingP2PWrapper.OnIntentionallyLeaveSession += Reset;
        matchmakingP2PWrapper.OnInvitedToSession += OnInvitedToGameSession;
        matchmakingP2PWrapper.OnRejectGameSessionCompleteEvent += OnSessionRejectedAsync;
    }

    private void OnInvitedToGameSession()
    {
        ShowLoading("Match Is Ready", "Cancelling match is timed out", 
            cancelMatchmakingTimeoutSec, RejectMatch,
            okCallback:AcceptMatch, okButtonText: "Accept", cancelButtonText: "Reject" );
    }

    private void UnbindMatchmakingEvents()
    {
        if (matchmakingP2PWrapper == null)
        {
            return;
        }
        
        matchmakingP2PWrapper.UnbindMatchmakingEvent();
        matchmakingP2PWrapper.OnMatchTicketP2PCreated -= OnMatchTicketP2PCreated;
        matchmakingP2PWrapper.OnMatchmakingWithP2PMatchFound += OnMatchFound;
        matchmakingP2PWrapper.OnMatchmakingWithP2PTicketExpired -= OnMatchmakingWithP2PTicketExpired;
        matchmakingP2PWrapper.OnMatchmakingWithP2PJoinSessionStarted -= OnMatchmakingWithP2PJoinSessionStarted;
        matchmakingP2PWrapper.OnMatchmakingWithP2PJoinSessionCompleted -= OnMatchmakingWithP2PJoinSessionCompleted;
        matchmakingP2PWrapper.OnMatchmakingWithP2PCanceled -= OnMatchmakingWithP2PCanceledAsync;
        matchmakingP2PWrapper.OnMatchmakingWithP2PError -= ErrorPanelAsync;
        matchmakingP2PWrapper.OnMatchmakingError -= ErrorPanelAsync;
        matchmakingP2PWrapper.OnIntentionallyLeaveSession -= Reset;
        matchmakingP2PWrapper.OnInvitedToSession -= OnInvitedToGameSession;
        matchmakingP2PWrapper.OnRejectGameSessionCompleteEvent -= OnSessionRejectedAsync;
    }

    private void CancelP2PMatchmaking()
    {
        matchmakingP2PWrapper.CancelP2PMatchmaking();
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void RejectMatch()
    {
        matchmakingP2PWrapper.RejectSessionInvitation();
        ShowLoading("Rejecting Match", "Rejecting Match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void AcceptMatch()
    {
        matchmakingP2PWrapper.AcceptSessionInvitation();
        ShowLoading("Joining Match", "Rejecting Match is timed out", cancelMatchmakingTimeoutSec);
    }


    private void OnMatchTicketP2PCreated()
    {
        switch (selectedGameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode:
                ShowLoading($"Finding {teamdeathmatchInfo}...",
                    $"Finding {teamdeathmatchInfo} is timed out",
                    matchmakingTimeoutSec, CancelP2PMatchmaking);
                break;
            case InGameMode.OnlineEliminationGameMode:
                ShowLoading($"Finding {eliminationInfo}...",
                    $"Finding {eliminationInfo} is timed out",
                    matchmakingTimeoutSec, CancelP2PMatchmaking);
                break;
        }
    }

    private void OnMatchFound()
    {
        ShowAdditionalInfo("Match Found", hideButton: true);
    }

    private async void OnMatchmakingWithP2PCanceledAsync()
    {
        await Task.Delay(1000);
        ShowInfo("Matchmaking is Canceled");
        Reset();
    }

    private async void OnSessionRejectedAsync(bool successed)
    {
        if (!successed)
        {
            CancelP2PMatchmaking();
        }

        ShowAdditionalInfo("Match Rejected");
        await Task.Delay(1000);
        ShowInfo("Match is rejected");
        Reset();
    }


    private void OnMatchmakingWithP2PJoinSessionCompleted(bool isLeader)
    {
        if (isLeader)
        {
            ShowLoading("Hosting P2P Game", "Hosting P2P Game timed out", joinSessionTimeoutSec, matchmakingP2PWrapper.LeaveCurrentSession, false);
        }
        else
        {
            ShowLoading("Waiting for P2P Host", "Waiting for for P2P Host timed out", joinSessionTimeoutSec, matchmakingP2PWrapper.LeaveCurrentSession, false);
        }
    }

    private void OnMatchmakingWithP2PJoinSessionStarted()
    {
        ShowLoading("Joining Match", "Joining Match is timed out", joinSessionTimeoutSec);
    }

    private void OnMatchmakingWithP2PTicketExpired()
    {
        ShowError("Matchmaking ticket is expired");
        Reset();
    }

    private void OnMatchmakingWithP2PMatchFound()
    {
        ShowLoading("Match Found", "Match Found timed out", joinSessionTimeoutSec);
    }

    private async void ErrorPanelAsync(string message)
    {
        await Task.Delay(1000);
        ShowError(message);
        Reset();
    }

    private void Reset()
    {
        selectedGameMode = InGameMode.None;
        UnbindMatchmakingEvents();
    }

    private async void OnMatchmakingWithDSCanceled()
    {
        await Task.Delay(1000);
        ShowInfo("Matchmaking is Canceled");
        Reset();
    }

    private void HideLoading(bool obj)
    {
        UnbindMatchmakingEvents();
        HideLoading();
    }

    #region MenuCanvas
    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingSessionP2P;
    }

    public override GameObject GetFirstButton()
    {
        return null;
    }
    #endregion
}
