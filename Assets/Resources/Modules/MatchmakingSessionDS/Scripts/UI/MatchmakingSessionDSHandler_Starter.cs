// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using UnityEngine;

public class MatchmakingSessionDSHandler_Starter : MenuCanvas
{
    private InGameMode selectedGameMode = InGameMode.None;
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
                break;
            case InGameMode.OnlineEliminationGameMode:
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
        //TODO: Copy your code here
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

    private async void ErrorPanel(string message)
    {
        await Delay();
        ShowError(message);
        Reset();
    }

    private async void OnMatchmakingWithDSCanceled()
    {
        await Delay();
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
        Reset();
    }

    private async Task Delay(int milliseconds=1000)
    {
        await Task.Delay(milliseconds);
    }

    private void CancelDSMatchmaking()
    {
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
    }

    private void ClientLeaveGameSession()
    {
        UnbindMatchmakingEvents();

        HideLoading();
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
