// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendDetailsMenuHandler_Starter : MenuCanvas
{
    [Header("Friend Details"), SerializeField] private Image friendImage;
    [SerializeField] private TMP_Text friendDisplayName;
    [SerializeField] private TMP_Text friendPresence;
    
    [Header("Friend Components"), SerializeField] private Button blockButton;
    [SerializeField] private Button unfriendButton;
    
    [Header("Party Components"), SerializeField] private Button promoteToLeaderButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button inviteToPartyButton;
    
    [Header("Menu Components"), SerializeField] private Button backButton;
    
    public Image FriendImage => friendImage;
    public TMP_Text FriendDisplayName => friendDisplayName;
    public TMP_Text FriendPresence => friendPresence;
    
    public string UserId { get; set; } = string.Empty;

    // TODO: Declare Module Wrappers here.

    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Update Party Buttons here.
    }
    
    private void Awake()
    {
        EnableButtonByModule(blockButton, TutorialType.ManagingFriends);
        EnableButtonByModule(unfriendButton, TutorialType.ManagingFriends);
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        blockButton.onClick.AddListener(BlockPlayer);
        unfriendButton.onClick.AddListener(Unfriend);
        
        promoteToLeaderButton.onClick.AddListener(PromoteToPartyLeader);
        kickButton.onClick.AddListener(KickFromParty);
        inviteToPartyButton.onClick.AddListener(InviteToParty);
        
        InitializePartyButtons(false);

        // TODO: Define Module Wrapper listeners here.
    }

    #region Manage Friends Module

    private void Unfriend()
    {
        // TODO: Implement Unfriend function here.
        BytewarsLogger.LogWarning("Unfriend is not yet implemented.");
    }
    
    private void BlockPlayer()
    {
        // TODO: Implement Block Player function here.
        BytewarsLogger.LogWarning("BlockPlayer is not yet implemented.");
    }

    private void OnUnfriendCompleted(Result result)
    {
        // TODO: Implement OnUnfriendCompleted function here.
        BytewarsLogger.LogWarning("OnUnfriendCompleted is not yet implemented.");
    }

    private void OnBlockPlayerComplete(Result<BlockPlayerResponse> result)
    {
        // TODO: Implement OnBlockPlayerComplete function here.
        BytewarsLogger.LogWarning("OnBlockPlayerComplete is not yet implemented.");
    }

    // TODO: Implement Friend Details functions here.

    #endregion Manage Friends Module

    #region Party Module

    #region Main Functions

    private void PromoteToPartyLeader()
    {
        BytewarsLogger.LogWarning("PromoteToPartyLeader is not yet implemented.");
    }
    
    private void KickFromParty()
    {
        BytewarsLogger.LogWarning("KickFromParty is not yet implemented.");
    }
    
    private void InviteToParty()
    {
        BytewarsLogger.LogWarning("InviteToParty is not yet implemented.");
    }

    #endregion Main Functions

    #region View Management

    private void InitializePartyButtons(bool inParty)
    {
        bool partyEssentialsActive = TutorialModuleManager.Instance.IsModuleActive(TutorialType.PartyEssentials);

        promoteToLeaderButton.gameObject.SetActive(partyEssentialsActive && !inParty);
        kickButton.gameObject.SetActive(partyEssentialsActive && !inParty);
        inviteToPartyButton.gameObject.SetActive(partyEssentialsActive && inParty);
    }

    // TODO: Implement Party view management functions here.

    #endregion View Management

    #endregion Party Module

    private static void EnableButtonByModule(Button button, TutorialType tutorialType)
    {
        bool moduleActive = TutorialModuleManager.Instance.IsModuleActive(tutorialType);
        
        button.gameObject.SetActive(moduleActive);
    }
    
    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendDetailsMenuCanvas_Starter;
    }
}
