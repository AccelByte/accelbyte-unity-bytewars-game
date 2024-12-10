// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class CreateMatchSessionP2PHandler_Starter : MenuCanvas
{
    private const int createMatchSessionTimeoutSec = 60;
    private const int joinMatchSessionTimeoutSec = 60;
    //TODO: Copy your MatchSessionP2PWrapper_Starter here

    public void ClickPeerToPeerButton()
    {
        //TODO: Copy your code here
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

        //TODO: Copy your code here
    }

    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionP2PHandler_Starter;
    }
    #endregion
}
