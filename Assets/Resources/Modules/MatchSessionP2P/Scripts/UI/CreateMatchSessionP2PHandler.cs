// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class CreateMatchSessionP2PHandler : MenuCanvas
{
    private const int createMatchSessionTimeoutSec = 60;
    private const int joinMatchSessionTimeoutSec = 60;

    private MatchSessionP2PWrapper matchSessionP2PWrapper;

    public void ClickPeerToPeerButton()
    {
        InGameMode selectedGameMode = InGameMode.None;

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

        matchSessionP2PWrapper.CreateMatchSession(selectedGameMode, GameSessionServerType.PeerToPeer);
        ShowLoading(
            "Creating Match Session (P2P)...",
            "Creating Match Session (P2P) is timed out.",
            createMatchSessionTimeoutSec,
            CancelCreateMatch);
    }

    private void InitWrapper()
    {
        if (matchSessionP2PWrapper == null)
        {
            matchSessionP2PWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionP2PWrapper>();
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
        matchSessionP2PWrapper.BindEvents();
        matchSessionP2PWrapper.BindMatchSessionP2PEvents();
        matchSessionP2PWrapper.OnCreatedMatchSession += OnCreatedMatchSession;
        matchSessionP2PWrapper.OnJoinedMatchSession += OnJoinedMatchSession;
        matchSessionP2PWrapper.OnLeaveSessionCompleted += UnbindMatchSessionEvent;
    }

    private void UnbindMatchSessionEvent()
    {
        matchSessionP2PWrapper.UnbindEvents();
        matchSessionP2PWrapper.UnbindMatchSessionP2PEvents();
        matchSessionP2PWrapper.OnCreatedMatchSession -= OnCreatedMatchSession;
        matchSessionP2PWrapper.OnJoinedMatchSession -= OnJoinedMatchSession;
        matchSessionP2PWrapper.OnLeaveSessionCompleted -= UnbindMatchSessionEvent;
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
        Reset();
        ShowInfo("Match session creation cancelled");
        matchSessionP2PWrapper.OnCreateMatchCancelled?.Invoke();
        matchSessionP2PWrapper.CancelCreateMatchSession();
    }

    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionP2PHandler;
    }
    #endregion
}
