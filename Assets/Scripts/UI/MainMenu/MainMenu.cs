#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MenuCanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button playOnlineBtn;
    [SerializeField] private Button helpAndOptionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private LayoutGroup layoutGroup;

    // Start is called before the first frame update
    void Start()
    {
        CheckModulesButtons();
        
        playButton.onClick.AddListener(OnPlayButtonPressed);
        playOnlineBtn.onClick.AddListener(OnPlayOnlineButtonPressed);
        helpAndOptionsButton.onClick.AddListener(OnHelpAndOptionsButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
        FixLayout();
    }

    private void OnPlayButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayMenuCanvas);
    }

    private void OnPlayOnlineButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayOnlineMenuCanvas);
    }

    public void OnHelpAndOptionsButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.HelpAndOptionsMenuCanvas);
    }

    public void OnQuitButtonPressed()
    {
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #else
        Application.Quit();
        #endif
    }

    public override GameObject GetFirstButton()
    {
        return playButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MainMenuCanvas;
    }


    private void CheckModulesButtons()
    {
        #if !BYTEWARS_DEBUG
        bool isOnlineBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingWithDS)
                                 || TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionWithDS);
        playOnlineBtn.gameObject.SetActive(isOnlineBtnActive);
        #endif
    }

    private async void FixLayout()
    {
        await Task.Delay(30);
        layoutGroup.enabled = false;
        await Task.Delay(30);
        layoutGroup.enabled = true;
    }
}

