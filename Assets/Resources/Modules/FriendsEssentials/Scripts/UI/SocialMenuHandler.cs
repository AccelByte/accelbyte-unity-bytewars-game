using UnityEngine;
using UnityEngine.UI;

public class SocialMenuHandler : MenuCanvas
{
    [SerializeField] private Button findFriendsButton;
    [SerializeField] private Button friendsButton;
    [SerializeField] private Button friendRequestsButton;
    [SerializeField] private Button sendFriendRequestsButton;
    [SerializeField] private Button partyButton;
    [SerializeField] private Button blockedPlayersButton;
    [SerializeField] private Button backButton;
    
    private void Start()
    {
        EnableButtonByModule(blockedPlayersButton, TutorialType.ManagingFriends);
        EnableButtonByModule(partyButton, TutorialType.PartyEssentials);

        findFriendsButton.onClick.AddListener(OnFindFriendButtonClicked);
        friendsButton.onClick.AddListener(OnFriendsButtonClicked);
        friendRequestsButton.onClick.AddListener(OnFriendRequestClicked);
        sendFriendRequestsButton.onClick.AddListener(OnSentFriendRequestClicked);
        partyButton.onClick.AddListener(OnPartyButtonClicked);
        blockedPlayersButton.onClick.AddListener(OnBlockedPlayersClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private static void EnableButtonByModule(Button button, TutorialType tutorialType)
    {
        bool moduleActive = TutorialModuleManager.Instance.IsModuleActive(tutorialType);

        button.gameObject.SetActive(moduleActive);
    }
    
    private static void OnSentFriendRequestClicked()
    {        
        MenuManager.Instance.ChangeToMenu(AssetEnum.SentFriendRequestsMenuCanvas);
    }

    private static void OnFriendRequestClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendRequestsMenuCanvas);
    }

    #region ButtonAction

    private static void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    private static void OnFindFriendButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FindFriendsMenuCanvas);
    }

    private static void OnFriendsButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendsMenuCanvas);
    }
    
    private static void OnPartyButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PartyMenuCanvas);
    }
    
    private static void OnBlockedPlayersClicked()
    {
        MenuManager.Instance.ChangeToMenu(IsStarterActive(TutorialType.ManagingFriends)
            ? AssetEnum.BlockedPlayersMenuCanvas_Starter
            : AssetEnum.BlockedPlayersMenuCanvas);
    }
    
    #endregion

    private static bool IsStarterActive(TutorialType tutorialType)
    {
        ModuleModel module = TutorialModuleManager.Instance.GetModule(tutorialType);

        return module.isStarterActive;
    }

    #region MenuCanvasOverride

    public override GameObject GetFirstButton()
    {
        return findFriendsButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SocialMenuCanvas;
    }

    #endregion

}
