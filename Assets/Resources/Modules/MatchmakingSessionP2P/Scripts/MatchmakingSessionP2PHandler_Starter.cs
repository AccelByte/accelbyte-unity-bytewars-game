// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class MatchmakingSessionP2PHandler_Starter : MenuCanvas
{
    private MatchmakingSessionP2PWrapper_Starter wrapper;
    private InGameMode selectedGameMode = InGameMode.None;
    private const string eliminationWithP2PMatchPoolName = "unity-elimination-p2p";
    private const string teamdeathmatchWithP2PMatchPoolName = "unity-teamdeathmatch-p2p";

    public void ClickPeerToPeerButton()
    {

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
            wrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionP2PWrapper_Starter>();
        }

        BindMatchmakingEvent();

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
        ShowLoading("Joining Match...",
            "Joining match timed out", 30, CancelMatchmaking);
    }

    private void OnError(string errorMessage)
    {
        UnbindMatchmakingEvents();
        ShowError(errorMessage);
    }

    private void OnCancelMatchmakingComplete()
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
