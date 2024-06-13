// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using UnityEngine;

public class CreateMatchSessionDSHandler : MenuCanvas
{
    private const int createMatchSessionTimeoutSec = 60;
    private const int joinMatchSessionTimeoutSec = 90;
    private MatchSessionDSWrapper matchSessionDSWrapper;

    public void ClickDedicatedServerButton()
    {
        InGameMode selectedGameMode = InGameMode.None;
        GameSessionServerType selectedSessionServerType =
            TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials) ?
            GameSessionServerType.DedicatedServerAMS :
            GameSessionServerType.DedicatedServer;

        MenuCanvas menu = MenuManager.Instance.GetCurrentMenu();
        if (menu is MatchSessionServerTypeSelection serverTypeSelection)
        {
            selectedGameMode = serverTypeSelection.SelectedGameMode;
            InitWrapper();
        }
        else
        {
            BytewarsLogger.LogWarning("Current menu is not server type selection menu while try to create match with DS");
            return;
        }

        matchSessionDSWrapper.CreateMatchSession(selectedGameMode, selectedSessionServerType);
        ShowLoading(
            "Creating Match Session (Dedicated Server)...",
            "Creating Match Session (Dedicated Server) is timed out.",
            createMatchSessionTimeoutSec,
            CancelCreateMatch);
    }

    private void InitWrapper()
    {
        if (matchSessionDSWrapper == null)
        {
            matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
        }

        BindMatchSessionEvent();

        MatchSessionServerTypeSelection.OnBackButtonCalled -= OnBackButtonFromServerSelection;
        MatchSessionServerTypeSelection.OnBackButtonCalled += OnBackButtonFromServerSelection;
    }

    private void Reset()
    {
        UnbindMatchSessionEvent();
    }

    private void OnBackButtonFromServerSelection()
    {
        UnbindMatchSessionEvent();
    }

    private void BindMatchSessionEvent()
    {
        matchSessionDSWrapper.BindEvents();
        matchSessionDSWrapper.BindMatchSessionDSEvents();
        matchSessionDSWrapper.OnCreatedMatchSession += OnCreatedMatchSession;
        matchSessionDSWrapper.OnJoinedMatchSession += OnJoinedMatchSession;
        matchSessionDSWrapper.OnLeaveSessionCompleted += UnbindMatchSessionEvent;
    }

    private void UnbindMatchSessionEvent()
    {
        matchSessionDSWrapper.UnbindEvents();
        matchSessionDSWrapper.UnbindMatchSessionDSEvents();
        matchSessionDSWrapper.OnCreatedMatchSession -= OnCreatedMatchSession;
        matchSessionDSWrapper.OnJoinedMatchSession -= OnJoinedMatchSession;
        matchSessionDSWrapper.OnLeaveSessionCompleted -= UnbindMatchSessionEvent;
    }

    private void OnCreatedMatchSession(bool isCreated)
    {
        if (isCreated)
        {
            ShowLoading(
                "Joining Session...",
                "Joining session is timed out.",
                joinMatchSessionTimeoutSec,
                CancelCreateMatch);
        }
        else
        {
            Reset();
            ShowError("Failed to create match session.");
        }
    }

    private void OnJoinedMatchSession(string errorMessage)
    {
        Reset();

        if (!string.IsNullOrEmpty(errorMessage))
        {
            ShowError($"Failed to join match session. {errorMessage}");
        }
    }

    private void CancelCreateMatch()
    {
        ShowInfo("Match session creation cancelled");
        matchSessionDSWrapper.OnCreateMatchCancelled?.Invoke();
        matchSessionDSWrapper.CancelCreateMatchSession();
    }

    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionDSHandler;
    }
    #endregion
}
