// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class MatchmakingSessionHandler : MenuCanvas
{
    [SerializeField]
    private Button eliminationButton;
    [SerializeField]
    private Button teamDeathMatchButton;
    [SerializeField]
    private Button backButton;
    private const string eliminationWithP2PMatchPoolName = "unity-elimination-p2p";
    private const string teamdeathmatchWithP2PMatchPoolName = "unity-teamdeathmatch-p2p";

    private void Start()
    {
        eliminationButton.onClick.AddListener(OnEliminationButtonClicked);
        teamDeathMatchButton.onClick.AddListener(OnTeamDeathMatchButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEliminationButtonClicked()
    {
        QuickPlayMatchmaking(InGameMode.OnlineEliminationGameMode);
    }

    private void OnTeamDeathMatchButtonClicked()
    {
        QuickPlayMatchmaking(InGameMode.OnlineDeathMatchGameMode);
    }

    private void QuickPlayMatchmaking(InGameMode inGameMode)
    {
        var menuCanvas = MenuManager.Instance.ChangeToMenu(AssetEnum.MatchmakingSessionServerTypeSelection);
        if (menuCanvas is MatchmakingSessionServerTypeSelection serverTypeSelection)
        {
            serverTypeSelection.SetInGameMode(inGameMode);
        }
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    #region MenuCanvas
    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingSessionMenuCanvas;
    }

    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }
    #endregion MenuCanvas
}
