using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class FriendDetailsMenuHandler_Starter : MenuCanvas
{
    public RectTransform friendDetailsPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button promoteToLeaderButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button inviteToPartyButton;
    [SerializeField] private Button blockButton;
    [SerializeField] private Button unfriendButton;

    private string _userId;
    public string UserID { private get => _userId;
        set
        {
            _userId = value;
            Debug.Log(UserID);
        }
    }
        
    // Start is called before the first frame update
    void Start()
    {
        EnableButton(blockButton, TutorialType.ManagingFriends);
        EnableButton(unfriendButton, TutorialType.ManagingFriends);
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(OnBlockCliked);
        unfriendButton.onClick.AddListener(OnUnfriendClicked);
        
        // Party-related buttons setup
        promoteToLeaderButton.gameObject.SetActive(false);
        kickButton.gameObject.SetActive(false);
        inviteToPartyButton.gameObject.SetActive(true);
        
        promoteToLeaderButton.onClick.AddListener(OnPromoteToLeaderButtonClicked);
        kickButton.onClick.AddListener(OnKickButtonClicked);
        inviteToPartyButton.onClick.AddListener(OnInviteToPartyButtonClicked);
    }
    
    private void EnableButton(Button button, TutorialType tutorialType)
    {
        var module = TutorialModuleManager.Instance.GetModule(tutorialType);
        if (module.isActive)
        {
            button.gameObject.SetActive(true);
        }
    }

    private void OnUnfriendClicked()
    {
        Debug.LogWarning($"Unfriend a friend is not yet implemented.");
    }
    
    private void OnBlockCliked()
    {
        Debug.LogWarning($"Block a player is not yet implemented.");
    }
    
    #region Party Functions

    private void OnPromoteToLeaderButtonClicked()
    {
        // add your code here
    }
    
    private void OnKickButtonClicked()
    {
        // add your code here
    }
    
    private void OnInviteToPartyButtonClicked()
    {
        // add your code here
    }

    #endregion
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenuCanvas_Starter;
    }
}
