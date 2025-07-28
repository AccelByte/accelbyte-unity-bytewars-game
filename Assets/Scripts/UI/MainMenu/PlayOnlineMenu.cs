// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayOnlineMenu : MenuCanvas
{
    [SerializeField] private Button createSessionButton;
    [SerializeField] private Button quickPlayButton;
    [SerializeField] private Button createMatchButton;
    [SerializeField] private Button browseMatchButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        SetModuleButtonVisibility();
        createSessionButton.onClick.AddListener(OnCreateSessionPressed);
        quickPlayButton.onClick.AddListener(OnQuickPlayButtonPressed);
        createMatchButton.onClick.AddListener(OnCreateMatchButtonPressed);
        browseMatchButton.onClick.AddListener(OnBrowserMatchButtonPressed);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void SetModuleButtonVisibility()
    {
#if !BYTEWARS_DEBUG
        bool isCreateSessionBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.SessionEssentials);
        bool isQuickPlayBtnActive = 
            TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingDSEssentials) || 
            TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingP2PEssentials);
        bool isCreateBrowseMatchBtnActive = 
            TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionDSEssentials) || 
            TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionP2PEssentials);

        createSessionButton.gameObject.SetActive(isCreateSessionBtnActive);
        quickPlayButton.gameObject.SetActive(isQuickPlayBtnActive);
        createMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
        browseMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
#endif
    }

    private void OnQuickPlayButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(TutorialType.MatchmakingEssentials);
    }

    private void OnCreateMatchButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.CreateMatchSessionMenu);
    }

    private void OnBrowserMatchButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.BrowseMatchMenu);
    }

    private void OnCreateSessionPressed()
    {
        ModuleModel module = TutorialModuleManager.Instance.GetModule(TutorialType.SessionEssentials);
        MenuManager.Instance.ChangeToMenu(
            module.isStarterActive ? AssetEnum.CreateSessionMenu_Starter : AssetEnum.CreateSessionMenu);
    }

    public override GameObject GetFirstButton()
    {
        Button[] buttons = new[] {
            createSessionButton,
            quickPlayButton,
            createMatchButton,
            browseMatchButton,
            backButton
        };
        return buttons.First(b => b.isActiveAndEnabled).gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PlayOnlineMenuCanvas;
    }
}
