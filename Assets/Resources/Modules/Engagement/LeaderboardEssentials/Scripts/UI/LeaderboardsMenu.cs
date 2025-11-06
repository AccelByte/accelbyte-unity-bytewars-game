// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class LeaderboardsMenu : MenuCanvas
{
    public static InGameMode SelectedGameMode { get; private set; }

    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button eliminationButton;
    [SerializeField] private Button teamDeathmatchButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        singlePlayerButton.onClick.AddListener(() => ChangeToLeaderboardPeriodMenu(InGameMode.SinglePlayer));
        eliminationButton.onClick.AddListener(() => ChangeToLeaderboardPeriodMenu(InGameMode.CreateMatchElimination));
        teamDeathmatchButton.onClick.AddListener(() => ChangeToLeaderboardPeriodMenu(InGameMode.CreateMatchTeamDeathmatch));
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void ChangeToLeaderboardPeriodMenu(InGameMode selectedGameMode)
    {
        SelectedGameMode = selectedGameMode;
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardPeriodMenu);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return singlePlayerButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardsMenu;
    }
}