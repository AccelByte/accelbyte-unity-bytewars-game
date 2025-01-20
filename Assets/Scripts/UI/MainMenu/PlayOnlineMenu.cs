// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class PlayOnlineMenu : MenuCanvas
{
    public Button backButton;
    public Button browseMatchButton;
    public Button createMatchButton;
    public Button quickPlayButton;
    public Button createSessionButton;


    // Start is called before the first frame update
    void Start()
    {
        SetModuleButtonVisibility();
        browseMatchButton.onClick.AddListener(OnBrowserMatchButtonPressed);
        createMatchButton.onClick.AddListener(OnCreateMatchButtonPressed);
        quickPlayButton.onClick.AddListener(OnQuickPlayButtonPressed);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        createSessionButton.onClick.AddListener(OnCreateSessionPressed);
    }

    private void SetModuleButtonVisibility()
    {
#if !BYTEWARS_DEBUG
        bool isCreateSessionBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.SessionEssentials);
        bool isQuickPlayBtnActive = (TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingWithDS) 
            || TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingWithP2P));
        bool isCreateBrowseMatchBtnActive = (TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionWithDS) 
            || TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionWithP2P));

        createSessionButton.gameObject.SetActive(isCreateSessionBtnActive);
        quickPlayButton.gameObject.SetActive(isQuickPlayBtnActive);
        createMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
        browseMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
#endif
    }


    private void OnQuickPlayButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(TutorialType.MatchmakingSession);
    }

    private void OnCreateMatchButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.MatchSessionHandler);
    }

    private void OnBrowserMatchButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.BrowseMatchMenuCanvas);
    }

    private void OnCreateSessionPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.SessionEssentialsMenuCanvas);
    }

    public override GameObject GetFirstButton()
    {
        return quickPlayButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PlayOnlineMenuCanvas;
    }
}
