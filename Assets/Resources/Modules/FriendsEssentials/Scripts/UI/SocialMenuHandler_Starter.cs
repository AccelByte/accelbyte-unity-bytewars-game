using UnityEngine;
using UnityEngine.UI;

public class SocialMenuHandler_Starter : MenuCanvas
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
        MenuManager.Instance.ChangeToMenu(AssetEnum.SentFriendRequestsMenuCanvas_Starter);
    }

    private static void OnFriendRequestClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendRequestsMenuCanvas_Starter);
    }

    #region ButtonAction

    private static void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    private static void OnFindFriendButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FindFriendsMenuCanvas_Starter);
    }

    private static void OnFriendsButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendsMenuCanvas_Starter);
    }

    private static void OnPartyButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PartyMenu_Starter);
    }
    
    private static void OnBlockedPlayersClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.BlockedPlayersMenuCanvas_Starter);
    }

    #endregion
    
    #region MenuCanvasOverride

    public override GameObject GetFirstButton()
    {
        return findFriendsButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SocialMenuCanvas_Starter;
    }

    #endregion

}
