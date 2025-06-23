using UnityEngine;
using UnityEngine.UI;

public class SocialMenu_Starter : MenuCanvas
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
        MenuManager.Instance.ChangeToMenu(AssetEnum.SentFriendRequestsMenu_Starter);
    }

    private static void OnFriendRequestClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendRequestsMenu_Starter);
    }

    #region ButtonAction

    private static void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    private static void OnFindFriendButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FindFriendsMenu_Starter);
    }

    private static void OnFriendsButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendsMenu_Starter);
    }

    private static void OnPartyButtonClicked()
    {
        ModuleModel partyEssentials = TutorialModuleManager.Instance.GetModule(TutorialType.PartyEssentials);
        if (partyEssentials != null)
        {
            AssetEnum menuAssetEnum = partyEssentials.isStarterActive ? AssetEnum.PartyMenu_Starter : AssetEnum.PartyMenu;
            MenuManager.Instance.ChangeToMenu(menuAssetEnum);
        }
    }
    
    private static void OnBlockedPlayersClicked()
    {
        ModuleModel managingFriends = TutorialModuleManager.Instance.GetModule(TutorialType.ManagingFriends);
        if (managingFriends != null)
        {
            AssetEnum menuAssetEnum = managingFriends.isStarterActive ? AssetEnum.BlockedPlayersMenu_Starter : AssetEnum.BlockedPlayersMenu;
            MenuManager.Instance.ChangeToMenu(menuAssetEnum);
        }
    }

    #endregion
    
    #region MenuCanvasOverride

    public override GameObject GetFirstButton()
    {
        return findFriendsButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SocialMenu_Starter;
    }

    #endregion

}
