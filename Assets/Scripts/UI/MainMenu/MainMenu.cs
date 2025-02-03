// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System;

public class MainMenu : MenuCanvas
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button playOnlineBtn;
    [SerializeField] private Button customMatchmakingButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button socialButton;
    [SerializeField] private Button helpAndOptionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private LayoutGroup layoutGroup;

    public static event Action<Action> OnQuitPressed;

    private void Start()
    {
        CheckModulesButtons();

        playButton.onClick.AddListener(OnPlayButtonPressed);
        playOnlineBtn.onClick.AddListener(OnPlayOnlineButtonPressed);
        customMatchmakingButton.onClick.AddListener(OnCustomMatchmakingButtonPressed);
        leaderboardButton.onClick.AddListener(OnLeaderboardButtonPressed);
        profileButton.onClick.AddListener(OnProfileButtonPressed);
        socialButton.onClick.AddListener(OnSocialButtonPressed);
        helpAndOptionsButton.onClick.AddListener(OnHelpAndOptionsButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
    }

    private static void OnSocialButtonPressed()
    {
        ModuleModel friendsEssentialModule = TutorialModuleManager.Instance.GetModule(TutorialType.FriendsEssentials);

        MenuManager.Instance.ChangeToMenu(friendsEssentialModule.isStarterActive 
            ? AssetEnum.SocialMenuCanvas_Starter : AssetEnum.SocialMenuCanvas);
    }
    
    private static void OnPlayButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayMenuCanvas);
    }

    private static void OnPlayOnlineButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PlayOnlineMenuCanvas);
    }
    
    private static void OnCustomMatchmakingButtonPressed() 
    {
        ModuleModel customMatchmakingModule = TutorialModuleManager.Instance.GetModule(TutorialType.CustomMatchmaking);
        MenuManager.Instance.ChangeToMenu(customMatchmakingModule.isStarterActive ? AssetEnum.CustomMatchmakingMenu_Starter : AssetEnum.CustomMatchmakingMenu);
    }

    private static void OnLeaderboardButtonPressed()
    {
        ModuleModel leaderboardEssentialModule = TutorialModuleManager.Instance.GetModule(TutorialType.LeaderboardEssentials);
        
        MenuManager.Instance.ChangeToMenu(leaderboardEssentialModule.isStarterActive 
            ? AssetEnum.LeaderboardSelectionMenuCanvas_Starter : AssetEnum.LeaderboardSelectionMenuCanvas);
    }
    
    private static void OnProfileButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.ProfileMenuCanvas);
    }
    
    private static void OnHelpAndOptionsButtonPressed()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.HelpAndOptionsMenuCanvas);
    }
    
    private static void OnQuitButtonPressed()
    {
        MenuManager.Instance.PromptMenu.ShowPromptMenu(
            header: "Quit Game", 
            message: "Are you sure you want to quit the game?", 
            cancelText: "No", 
            cancelAction: null,
            confirmText: "Yes", 
            confirmAction: () =>
            {
                bool authEssentialsActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.AuthEssentials);
                if (authEssentialsActive)
                {
                    OnQuitPressed?.Invoke(QuitGame);
                }
                else
                {
                    QuitGame();
                }
            });
    }

    private static void QuitGame()
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
                                 || TutorialModuleManager.Instance.IsModuleActive(TutorialType.MatchSessionWithDS)
                                 || TutorialModuleManager.Instance.IsModuleActive(TutorialType.SessionEssentials);
        playOnlineBtn.gameObject.SetActive(isOnlineBtnActive);
#endif
        bool isCustomMatchmakingModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.CustomMatchmaking);
        customMatchmakingButton.gameObject.SetActive(isCustomMatchmakingModuleActive);

        bool isFriendModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.FriendsEssentials);
        socialButton.gameObject.SetActive(isFriendModuleActive);

        bool isStatsModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.StatsEssentials);
        profileButton.gameObject.SetActive(isStatsModuleActive);

        bool isLeaderboardModuleActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.LeaderboardEssentials);
        leaderboardButton.gameObject.SetActive(isLeaderboardModuleActive);
    }
}

