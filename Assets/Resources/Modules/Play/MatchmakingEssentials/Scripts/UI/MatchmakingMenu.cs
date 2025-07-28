// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class MatchmakingMenu : MenuCanvas
{
    public static InGameMode SelectedGameMode { get; private set; }

    [SerializeField] private Button eliminationButton;
    [SerializeField] private Button teamDeathMatchButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        eliminationButton.onClick.AddListener(() => ChangeToServerTypeMenu(InGameMode.MatchmakingElimination));
        teamDeathMatchButton.onClick.AddListener(() => ChangeToServerTypeMenu(InGameMode.MatchmakingTeamDeathmatch));
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void ChangeToServerTypeMenu(InGameMode gameMode)
    {
        SelectedGameMode = gameMode;
        MenuManager.Instance.ChangeToMenu(AssetEnum.MatchmakingServerTypeMenu);
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingMenu;
    }

    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }
}
