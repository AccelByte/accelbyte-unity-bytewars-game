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
        var isCreateSessionBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.SessionEssentials);
        var isQuickPlayBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingWithDS);
        var isCreateBrowseMatchBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionWithDS);
        
        createSessionButton.gameObject.SetActive(isCreateSessionBtnActive);
        quickPlayButton.gameObject.SetActive(isQuickPlayBtnActive);
        createMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
        browseMatchButton.gameObject.SetActive(isCreateBrowseMatchBtnActive);
        #endif
    }


    private void OnQuickPlayButtonPressed()
    {
        // MenuManager.Instance.ChangeToMenu(AssetEnum.ServerTypeSelection);
        //TODO delete this and uncomment code above to enable peer to peer server selection
        GameData.ServerType = ServerType.OnlineDedicatedServer;
        MenuManager.Instance.ChangeToMenu(AssetEnum.QuickPlayMenuCanvas);
    }

    private void OnCreateMatchButtonPressed()
    {
       MenuManager.Instance.ChangeToMenu(AssetEnum.CreateMatchMenuCanvas);
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
