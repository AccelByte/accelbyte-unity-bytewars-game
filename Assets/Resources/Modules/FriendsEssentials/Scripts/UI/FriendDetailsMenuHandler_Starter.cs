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

        InitializePartyButtons();

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

    private void InitializePartyButtons()
    {
        inviteToPartyButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnInviteToPartyButtonClicked(UserId); });
        promoteToLeaderButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnPromotePartyLeaderButtonClicked(UserId); });
        kickButton.onClick.AddListener(() => { PartyEssentialsModels.PartyHelper.OnKickPlayerFromPartyButtonClicked(UserId); });

        PartyEssentialsModels.PartyHelper.BindOnPartyUpdate(UpdatePartyButtons);

        // Update party button states after initialization.
        UpdatePartyButtons();
    }

    private void UpdatePartyButtons()
    {
        ModuleModel partyModule = TutorialModuleManager.Instance.GetModule(TutorialType.PartyEssentials);
        bool isPartyModuleActive = partyModule != null && partyModule.isActive;

        SessionV2PartySession partySession = PartyEssentialsModels.PartyHelper.CurrentPartySession;
        PlayerState currentUser = GameData.CachedPlayerState;

        bool isFriendInParty = false, isCurrentUserIsLeader = false;
        if (currentUser != null)
        {
            isFriendInParty =
                partySession == null ? false :
                partySession.members.
                Where(x => x.StatusV2 == SessionV2MemberStatus.JOINED).
                Select(x => x.id).Contains(UserId);

            isCurrentUserIsLeader =
                partySession != null &&
                partySession.leaderId == (currentUser == null ? string.Empty : currentUser.playerId);
        }

        inviteToPartyButton.gameObject.SetActive(isPartyModuleActive && !isFriendInParty);
        promoteToLeaderButton.gameObject.SetActive(isPartyModuleActive && isFriendInParty && isCurrentUserIsLeader);
        kickButton.gameObject.SetActive(isPartyModuleActive && isFriendInParty && isCurrentUserIsLeader);
    }

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
