// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class CreateMatchSessionDSHandler_Starter : MenuCanvas
{
    private const int createMatchSessionTimeoutSec = 60;
    private const int joinMatchSessionTimeoutSec = 60;

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

        //TODO: Copy your code here

        ShowLoading(
            "Creating Match Session (Dedicated Server)...",
            "Creating Match Session (Dedicated Server) is timed out.",
            createMatchSessionTimeoutSec,
            CancelCreateMatch);
    }

    private void InitWrapper()
    {
        //TODO: Copy your code here

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
        //TODO: Copy your code here
    }

    private void UnbindMatchSessionEvent()
    {
        //TODO: Copy your code here
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
        HideLoading();

        //TODO: Copy your code here
    }

    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionDSHandler_Starter;
    }
    #endregion
}
