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

    
    // Start is called before the first frame update
    void Start()
    {
        EnableButton(blockedPlayersButton, TutorialType.ManagingFriends);
        EnableButton(partyButton, TutorialType.PartyEssentials);

        findFriendsButton.onClick.AddListener(OnFindFriendButtonClicked);
        friendsButton.onClick.AddListener(OnFriendsButtonClicked);
        friendRequestsButton.onClick.AddListener(OnFriendRequestClicked);
        sendFriendRequestsButton.onClick.AddListener(OnSentFriendRequestClicked);
        partyButton.onClick.AddListener(OnPartyButtonClicked);
        blockedPlayersButton.onClick.AddListener(OnBlockedPlayersClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void EnableButton(Button button, TutorialType tutorialType)
    {
        var module = TutorialModuleManager.Instance.GetModule(tutorialType);
        if (module.isActive)
        {
            button.gameObject.SetActive(true);
        }
    }
    
    private void OnSentFriendRequestClicked()
    {        
        MenuManager.Instance.ChangeToMenu(AssetEnum.SentFriendRequestMenuCanvas);
    }

    private void OnFriendRequestClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendRequestMenuCanvas);
    }

    #region ButtonAction

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    private void OnFindFriendButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FindFriendsMenuCanvas);
    }

    private void OnFriendsButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendMenuCanvas);
    }
    
    private void OnPartyButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.PartyMenuCanvas);
    }
    
    private void OnBlockedPlayersClicked()
    {
        if (IsStarterActive())
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.BlockedPlayersMenuCanvas_Starter);
        }
        else
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.BlockedPlayersMenuCanvas);
        }
        
    }
    
    #endregion

    private bool IsStarterActive()
    {
        var module = TutorialModuleManager.Instance.GetModule(TutorialType.ManagingFriends);
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
