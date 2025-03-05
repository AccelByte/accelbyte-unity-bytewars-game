// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class MatchSessionHandler : MenuCanvas
{
    [SerializeField]
    private Button eliminationButton;
    
    [SerializeField]
    private Button teamDeathMatchButton;

    [SerializeField]
    private Button backButton;

    private void Start()
    {
        eliminationButton.onClick.AddListener(OnEliminationButtonClicked);
        teamDeathMatchButton.onClick.AddListener(OnTeamDeathMatchButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnDestroy()
    {
        eliminationButton.onClick.RemoveAllListeners();
        teamDeathMatchButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private void OnEliminationButtonClicked()
    {
        SetMatchSessionGameMode(InGameMode.CreateMatchElimination);
    }

    private void OnTeamDeathMatchButtonClicked()
    {
        SetMatchSessionGameMode(InGameMode.CreateMatchTeamDeathmatch);
    }

    private void SetMatchSessionGameMode(InGameMode inGameMode)
    {
        MenuCanvas menuCanvas = MenuManager.Instance.ChangeToMenu(AssetEnum.MatchSessionServerTypeSelection);
        if (menuCanvas is MatchSessionServerTypeSelection serverTypeSelection)
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
        return AssetEnum.MatchSessionHandler;
    }

    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }
    #endregion MenuCanvas
}
