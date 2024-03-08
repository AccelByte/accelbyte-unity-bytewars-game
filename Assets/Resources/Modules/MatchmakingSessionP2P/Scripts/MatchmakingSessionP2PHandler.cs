// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;

public class MatchmakingSessionP2PHandler : MenuCanvas
{
    private MatchmakingSessionP2PWrapper wrapper;
    private InGameMode selectedGameMode = InGameMode.None;
    private const string eliminationWithP2PMatchPoolName = "unity-elimination-p2p";
    private const string teamdeathmatchWithP2PMatchPoolName = "unity-teamdeathmatch-p2p";
    private const int matchQueueTimeOffsetSec = 10;
    private const string eliminationInfo = "Elimination Match (Peer To Peer)";
    private const string teamdeathmatchInfo = "Team Death Match (Peer To Peer)";
    private const int createMatchTicketSec = 20;
    private bool isMatchFound;

    public void ClickPeerToPeerButton()
    {
        var menu = MenuManager.Instance.GetCurrentMenu();
        if (menu is MatchmakingSessionServerTypeSelection serverTypeSelection)
        {
            InitWrapper();
            selectedGameMode = serverTypeSelection.SelectedGameMode;
        }
        else
        {
            BytewarsLogger.LogWarning("Current menu is not server type selection menu while try to matchmaking with Peer to Peer");
        }
        isMatchFound = false;
        switch (selectedGameMode)
        {
            case InGameMode.OnlineDeathMatchGameMode:
                wrapper.StartMatchmaking(teamdeathmatchWithP2PMatchPoolName, selectedGameMode,
                    OnStartTeamdeathmatchCompleted);
                ShowLoading($"Creating {teamdeathmatchInfo}...",
                    $"Creating {teamdeathmatchInfo} is timed out",
                    createMatchTicketSec, CancelMatchmaking);
                break;
            case InGameMode.OnlineEliminationGameMode:
                wrapper.StartMatchmaking(eliminationWithP2PMatchPoolName, selectedGameMode,
                    OnStartEliminationCompleted);
                ShowLoading($"Creating {eliminationInfo}...",
                    $"Creating {eliminationInfo} is timed out",
                    createMatchTicketSec, CancelMatchmaking);
                break;
            default:
                string errorMsg = $"No Peer To Peer MatchPoolName for {selectedGameMode}";
                BytewarsLogger.LogWarning(errorMsg);
                ShowError(errorMsg);
                break;
        }
    }

    private void OnStartTeamdeathmatchCompleted(int queueTimeSec)
    {
        if (isMatchFound)
        {
            return;
        }
        ShowLoading($"Finding {teamdeathmatchInfo}...",
                    $"Finding {teamdeathmatchInfo} is timed out",
                    queueTimeSec + matchQueueTimeOffsetSec, CancelMatchmaking);
    }

    private void OnStartEliminationCompleted(int queueTimeSec)
    {
        if (isMatchFound)
        {
            return;
        }
        ShowLoading($"Finding {eliminationInfo}...",
                    $"Finding {eliminationInfo} is timed out",
                    queueTimeSec + matchQueueTimeOffsetSec, CancelMatchmaking);
    }

    private void CancelMatchmaking()
    {
        wrapper.CancelMatchmaking();
        ShowLoading("Cancelling Match", "Cancelling match is timed out", 30);
    }

    private void InitWrapper()
    {
        if (wrapper == null)
        {
            wrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionP2PWrapper>();
            BindMatchmakingEvent();
        }
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled -= OnBackButtonFromServerSelection;
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled += OnBackButtonFromServerSelection;
    }

    private void BindMatchmakingEvent()
    {
        if (wrapper == null)
        {
            return;
        }
        wrapper.OnCancelMatchmakingComplete += OnCancelMatchmakingComplete;
        wrapper.OnMatchFound += OnMatchFound;
        wrapper.OnError += OnError;
        wrapper.OnStartingP2PConnection += OnStartingP2PConnection;
    }

    private void UnbindMatchmakingEvents()
    {
        if (wrapper == null)
        {
            return;
        }
        wrapper.OnCancelMatchmakingComplete -= OnCancelMatchmakingComplete;
        wrapper.OnMatchFound -= OnMatchFound;
        wrapper.OnError -= OnError;
        wrapper.OnStartingP2PConnection -= OnStartingP2PConnection;
    }

    private void OnStartingP2PConnection()
    {
        UnbindMatchmakingEvents();
    }

    private void OnBackButtonFromServerSelection()
    {
        UnbindMatchmakingEvents();
    }

    private void OnMatchFound()
    {
        isMatchFound = true;
        ShowLoading("Joining Match...",
            "Joining match timed out", 30, CancelMatchmaking);
    }

    private void OnError(string errorMessage)
    {
        ShowError(errorMessage);
    }

    private void OnCancelMatchmakingComplete()
    {
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
