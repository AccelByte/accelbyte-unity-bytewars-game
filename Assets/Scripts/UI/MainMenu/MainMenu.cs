#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MenuCanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button playOnlineBtn;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button socialButton;
    [SerializeField] private Button helpAndOptionsButton;
    [SerializeField] private Button quitButton;

    // Start is called before the first frame update
    void Start()
    {
        CheckModulesButtons();
        
        playButton.onClick.AddListener(OnPlayButtonPressed);
        playOnlineBtn.onClick.AddListener(OnPlayOnlineButtonPressed);
        leaderboardButton.onClick.AddListener(OnLeaderboardButtonPressed);
        profileButton.onClick.AddListener(OnProfileButtonPressed);
        socialButton.onClick.AddListener(OnSocialButtonPressed);
        helpAndOptionsButton.onClick.AddListener(OnHelpAndOptionsButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
    }

    private void OnSocialButtonPressed()
    {
        var friendEssentialModule = TutorialModuleManager.Instance.GetModule(TutorialType.FriendEssentials);
        if (!friendEssentialModule.isStarterActive)
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.SocialMenuCanvas);
        }
        else
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.SocialMenuCanvas_Starter);
        }
    }

    public void OnPlayButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayMenuCanvas);
    }

    public void OnPlayOnlineButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayOnlineMenuCanvas);
    }
    
    public void OnLeaderboardButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardsMenuCanvas);
    }
    
    public void OnProfileButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.ProfileMenuCanvas);
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
        bool isOnlineBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingEssentials);
        playOnlineBtn.gameObject.SetActive(isOnlineBtnActive);
        #endif
    }

    private void CheckModulesButtons()
    {
        #if !BYTEWARS_DEBUG
            bool isOnlineBtnActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchmakingEssentials)
                || TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSession);
            playOnlineBtn.gameObject.SetActive(isOnlineBtnActive);
        #endif

        bool isFriendModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.FriendEssentials);
        socialButton.gameObject.SetActive(isFriendModuleActive);
        
        bool isStatsModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.StatsEssentials);
        profileButton.gameObject.SetActive(isStatsModuleActive);

        bool isLeaderboarModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.LeaderboardEssentials);
        leaderboardButton.gameObject.SetActive(isLeaderboarModuleActive);
    }
}
