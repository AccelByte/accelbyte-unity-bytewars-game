// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class MatchmakingSessionDSHandler_Starter : MenuCanvas
{
    private InGameMode selectedGameMode = InGameMode.None;
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;
    //TODO: Copy MatchmakingSessionDSWrapper_Starter here

    public void ClickDedicatedServerButton()
    {
        //TODO: Copy Your code here
    }

    private void InitWrapper()
    {
        //TODO: Copy Your code here


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
        //TODO: Copy your code here
    }

    private void OnMatchTicketDSCreated()
    {
        switch (selectedGameMode)
        {
            case InGameMode.MatchmakingTeamDeathmatch:
                ShowLoading("Finding Team Death Match (Dedicated Server)...",
                "Finding Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
            case InGameMode.MatchmakingElimination:
                ShowLoading("Finding Elimination Match (Dedicated Server)...",
                "Elimination Team Death Match (Dedicated Server) is timed out",
                matchmakingTimeoutSec, CancelDSMatchmaking);
                break;
        }
    }

    private void UnbindMatchmakingEvents()
    {
        //TODO: Copy your code here
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
            okCallback: AcceptMatch, okButtonText: "Accept", cancelButtonText: "Reject");
    }

    private void OnMatchmakingWithDSJoinSessionStarted()
    {
        ShowLoading("Joining Match", "Joining Match is timed out", joinSessionTimeoutSec);
    }

    private void OnMatchmakingWithDSJoinSessionCompleted()
    {
        ShowLoading("Waiting for DS", "Waiting for DS timed out", joinSessionTimeoutSec, OnDSTimeOut, false);
    }

    private void Travelling(bool isAvailable)
    {
        ShowLoading("Travelling", "Match Found timed out", joinSessionTimeoutSec);
        Reset();
    }

    private void OnMatchmakingWithDSTicketExpired()
    {
        ShowError("Matchmaking ticket is expired");
        Reset();
    }

    private void OnMatchmakingWithDSMatchFound()
    {
        ShowLoading("Match Found", "Match Found timed out", joinSessionTimeoutSec);
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
        //TODO: Copy your code here
        Reset();
    }

    private void CancelDSMatchmaking()
    {
        //TODO: Copy your code here
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void RejectMatch()
    {
        //TODO: Copy your code here
        ShowLoading("Rejecting Match", "Rejecting Match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void AcceptMatch()
    {
        //TODO: Copy your code here
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
