﻿// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using UnityEngine;

public class MatchmakingSessionP2PHandler_Starter : MenuCanvas
{
    //TODO: Copy your MatchmakingSessionP2PWrapper_Starter here
    private InGameMode selectedGameMode = InGameMode.None;
    private const string eliminationInfo = "Elimination Match (Peer To Peer)";
    private const string teamdeathmatchInfo = "Team Death Match (Peer To Peer)";
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;

    public void ClickPeerToPeerButton()
    {
        //TODO: Copy your code here
    }

    private void InitWrapper()
    {
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

    private void UnbindMatchmakingEvents()
    {
        //TODO: Copy your code here
    }

    private void CancelP2PMatchmaking()
    {
        //TODO: Copy your code here
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
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


    private async void OnMatchmakingWithP2PCanceledAsync()
    {
        await Delay();
        ShowInfo("Matchmaking is Canceled");
        Reset();
    }

    private void OnMatchmakingWithP2PJoinSessionCompleted(bool isLeader)
    {
        //TODO: Copy your code here
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
        await Delay();
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
        await Delay();
        ShowInfo("Matchmaking is Canceled");
        Reset();
    }


    private async Task Delay()
    {
        await Task.Delay(1500);
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