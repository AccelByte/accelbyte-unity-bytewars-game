// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MultiplayerMenu : MenuCanvas
{
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Button eliminationButton;
    [SerializeField]
    private Button teamDeadMatchButton;

    [SerializeField] private GameModeSO teamDeathMatchGameMode;


    // Start is called before the first frame update
    void Start()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        eliminationButton.onClick.AddListener(ClickEliminationBtn);
        teamDeadMatchButton.onClick.AddListener(ClickTeamDeathMatch);
    }

    private void ClickTeamDeathMatch()
    {
        GameManager.Instance.StartGame(teamDeathMatchGameMode);
    }

    private void ClickEliminationBtn()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.EliminationMenuCanvas);
    }

    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MultiplayerMenuCanvas;
    }
}
