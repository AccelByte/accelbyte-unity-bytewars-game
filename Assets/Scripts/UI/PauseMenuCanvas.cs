// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class PauseMenuCanvas : MenuCanvas
{
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button restartBtn;
    [SerializeField] private Button playerListBtn;
    [SerializeField] private Button quitBtn;
    
    private bool isRestartBtnShown;

    private void Start()
    {
        isRestartBtnShown = true;

        resumeBtn.onClick.AddListener(OnClickResumeBtn);
        restartBtn.onClick.AddListener(GameManager.Instance.RestartLocalGame);
        playerListBtn.onClick.AddListener(OnPlayerListClicked);
        quitBtn.onClick.AddListener(OnQuitBtnClick);
    }
    private void OnPlayerListClicked()
    {
        GameManager.Instance.InGamePause.ShowInGamePauseMenu(AssetEnum.PlayerListMenu);
    }

    private void OnEnable()
    {
        restartBtn.gameObject.SetActive(isRestartBtnShown);

        if (GameData.GameModeSo && GameData.GameModeSo.GameMode == GameModeEnum.OnlineMultiplayer)
        {
            playerListBtn.gameObject.SetActive(TutorialModuleManager.Instance.IsModuleActive(TutorialType.RecentPlayers));
        }
    }

    private void OnDisable()
    {
        isRestartBtnShown = true;
    }

    public override GameObject GetFirstButton()
    {
        return resumeBtn.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PauseMenuCanvas;
    }

    private void OnQuitBtnClick()
    {
        StartCoroutine(GameManager.Instance.QuitToMainMenu());
    }

    private void OnClickResumeBtn()
    {
        GameManager.Instance.InGamePause.ToggleGamePause();
    }

    public void DisableRestartBtn()
    {
        isRestartBtnShown = false;
        restartBtn.gameObject.SetActive(false);
    }
}
