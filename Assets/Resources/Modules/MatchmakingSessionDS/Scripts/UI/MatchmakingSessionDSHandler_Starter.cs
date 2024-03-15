// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionDSHandler_Starter : MenuCanvas
{
    private const string eliminationWithDSMatchPoolName = "unity-elimination-ds";
    private const string teamdeathmatchWithDSMatchPoolName = "unity-teamdeathmatch-ds";
    private const string eliminationDSAMSMatchPool = "unity-elimination-ds-ams";
    private const string teamDeathmatchDSAMSMatchPool = "unity-teamdeathmatch-ds-ams";
    private InGameMode selectedGameMode = InGameMode.None;
    private MatchmakingSessionDSWrapper_Starter matchmakingSessionDSWrapper;
    private const int matchmakingTimeoutSec = 90;
    private const int joinSessionTimeoutSec = 60;
    private const int cancelMatchmakingTimeoutSec = 30;

    public void ClickDedicatedServerButton()
    {
        BytewarsLogger.Log("Matchmaking with DS is not implemented yet!!!");
    }

    private void InitWrapper()
    {
        if (matchmakingSessionDSWrapper == null)
        {
            matchmakingSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper_Starter>();

            if (matchmakingSessionDSWrapper != null)
            {
                matchmakingSessionDSWrapper.BindEventListener();
                BindMatchmakingEvent();
            }
        }
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled -= OnBackButtonFromServerSelection;
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled += OnBackButtonFromServerSelection;
    }

    private void OnBackButtonFromServerSelection()
    {
        UnbindMatchmakingEvents();
        matchmakingSessionDSWrapper?.UnbindEventListener();
    }

    private void BindMatchmakingEvent()
    {
        // listen event when match is found and ds available
        matchmakingSessionDSWrapper.OnMatchmakingFoundEvent += JoinSessionPanel;
        matchmakingSessionDSWrapper.OnDSAvailableEvent += TravelToGame;

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
        matchmakingSessionDSWrapper.OnMatchmakingFoundEvent -= JoinSessionPanel;
        matchmakingSessionDSWrapper.OnDSAvailableEvent -= TravelToGame;
        matchmakingSessionDSWrapper.OnStartMatchmakingFailed -= FailedPanel;
        matchmakingSessionDSWrapper.OnMatchmakingJoinSessionFailedEvent += FailedPanel;
        matchmakingSessionDSWrapper.OnDSFailedRequestEvent -= FailedPanel;
        matchmakingSessionDSWrapper.OnSessionEnded -= FailedPanel;
        matchmakingSessionDSWrapper.OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    private void FailedPanel()
    {
        ShowError("Cannot find Match");
    }

    private void OnCancelMatchmakingComplete()
    {
        HideLoading();
    }

    private void TravelToGame(SessionV2GameSession session)
    {
        matchmakingSessionDSWrapper.TravelToDS(session, selectedGameMode);
        UnbindMatchmakingEvents();
        matchmakingSessionDSWrapper.UnbindEventListener();
    }

    private void JoinSessionPanel(string sessionId)
    {
        ShowLoading("Joining Match", "Joining Match is timed out", joinSessionTimeoutSec);
    }

    private void CancelDSMatchmaking()
    {
        matchmakingSessionDSWrapper.CancelDSMatchmaking();
        ShowLoading("Cancelling Match", "Cancelling match is timed out", cancelMatchmakingTimeoutSec);
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
