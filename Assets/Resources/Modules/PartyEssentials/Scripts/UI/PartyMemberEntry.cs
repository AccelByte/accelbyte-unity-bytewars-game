// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberEntry : MonoBehaviour
{
    [SerializeField] private Image outlineImage;
    [SerializeField] private AccelByteWarsAsyncImage avatarImage;
    [SerializeField] private TMP_Text memberNameText;
    [SerializeField] private Button memberButton;
    [SerializeField] private Button addMemberButton;

    [SerializeField] private Color leaderColor;
    [SerializeField] private Color memberColor;

    private AccelByte.Models.AccountUserPlatformData cachedMemberUserData = null;

    public void SetPartyMember(AccelByte.Models.AccountUserPlatformData memberUserData, bool isLeader)
    {
        cachedMemberUserData = memberUserData;

        SetPartyMemberColor(isLeader ? leaderColor : memberColor);
        memberNameText.text = string.IsNullOrEmpty(memberUserData.DisplayName) ?
            AccelByteWarsUtility.GetDefaultDisplayNameByUserId(memberUserData.UserId) : memberUserData.DisplayName;
        avatarImage.LoadImage(memberUserData.AvatarUrl);

        // Switch view to display member information.
        memberButton.gameObject.SetActive(true);
        addMemberButton.gameObject.SetActive(false);

        // Only set button interaction for non-current user entry.
        SetInteractable(GameData.CachedPlayerState.PlayerId != memberUserData.UserId);
    }

    public void ResetPartyMember() 
    {
        memberNameText.text = string.Empty;
        avatarImage.ResetImage();
        SetPartyMemberColor(Color.white);

        // Switch view to display add member button.
        memberButton.gameObject.SetActive(false);
        addMemberButton.gameObject.SetActive(true);
        SetInteractable(true);
    }

    private void OnEnable()
    {
        addMemberButton.onClick.AddListener(AddPartyMember);
        memberButton.onClick.AddListener(OpenPlayerActionMenu);
    }

    private void OnDisable()
    {
        addMemberButton.onClick.RemoveAllListeners();
        memberButton.onClick.RemoveAllListeners();
    }

    private void AddPartyMember()
    {
        // Open friend list menu to add new party members.
        ModuleModel friendEssentials = TutorialModuleManager.Instance.GetModule(TutorialType.FriendsEssentials);
        if (friendEssentials != null)
        {
            AssetEnum menuAssetEnum = friendEssentials.isStarterActive ? AssetEnum.FriendsMenu_Starter : AssetEnum.FriendsMenu;
            MenuManager.Instance.ChangeToMenu(menuAssetEnum);
        }
    }

    private void OpenPlayerActionMenu()
    {
        if (cachedMemberUserData == null) 
        {
            BytewarsLogger.LogWarning($"Failed to open player action menu. Invalid party member user data.");
            return;
        }

        ModuleModel friendEssentials = TutorialModuleManager.Instance.GetModule(TutorialType.FriendsEssentials);
        if (friendEssentials == null || !friendEssentials.isActive) 
        {
            BytewarsLogger.LogWarning($"Failed to open player action menu. Friend Essentials module is not active.");
            return;
        }

        // Open friend details menu to perform party actions (e.g. promote to leader, kick, etc.)
        AssetEnum menuAssetEnum = friendEssentials.isStarterActive ? AssetEnum.FriendDetailsMenu_Starter : AssetEnum.FriendDetailsMenu;
        MenuManager.Instance.InstantiateCanvas(menuAssetEnum);
        if (!MenuManager.Instance.AllMenu.TryGetValue(menuAssetEnum, out MenuCanvas menuCanvas))
        {
            BytewarsLogger.LogWarning($"Failed to open player action menu. Unable to find {menuAssetEnum} in menu manager.");
            return;
        }
        if (menuCanvas.gameObject.TryGetComponent(out FriendDetailsMenu friendDetailsMenu))
        {
            friendDetailsMenu.UserId = cachedMemberUserData.UserId;
            friendDetailsMenu.FriendImage.sprite = avatarImage.GetCurrentImage();
            friendDetailsMenu.FriendDisplayName.text = memberNameText.text;
            friendDetailsMenu.FriendPresence.text = "Online";
        }
        else if (menuCanvas.gameObject.TryGetComponent(out FriendDetailsMenu_Starter friendDetailsMenuStarter))
        {
            friendDetailsMenuStarter.UserId = cachedMemberUserData.UserId;
            friendDetailsMenuStarter.FriendImage.sprite = avatarImage.GetCurrentImage();
            friendDetailsMenuStarter.FriendDisplayName.text = memberNameText.text;
            friendDetailsMenuStarter.FriendPresence.text = "Online";
        }

        MenuManager.Instance.ChangeToMenu(menuAssetEnum);
    }

    private void SetPartyMemberColor(Color color)
    {
        avatarImage.SetImageTint(color);
        memberNameText.color = color;
        outlineImage.color = color;
    }

    private void SetInteractable(bool isInteractable)
    {
        memberButton.interactable = isInteractable;
        addMemberButton.interactable = isInteractable;
    }
}
