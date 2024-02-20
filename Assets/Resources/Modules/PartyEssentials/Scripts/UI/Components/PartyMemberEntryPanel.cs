// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PartyMemberEntryPanel : MonoBehaviour
{
    [SerializeField]
    private Button addMemberButton;
    [SerializeField]
    private Button memberInfoPanel;
    [SerializeField]
    private Image playerAvatarPanelImage;
    [SerializeField]
    private Image avatarImage;
    [SerializeField]
    private TMP_Text playerNameText;

    private string currentUserId;

    private void Start()
    {
        addMemberButton.onClick.AddListener(OnAddMemberButtonClicked);
        memberInfoPanel.onClick.AddListener(OnMemberInfoPanelClicked);
    }

    private void OnAddMemberButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendMenuCanvas);
    }

    private void OnMemberInfoPanelClicked()
    {
        // Trigger Friend Details Menu
        MenuCanvas friendDetailsMenu = MenuManager.Instance.GetMenu(AssetEnum.FriendDetailsMenuCanvas);
        FriendDetailsMenuHandler friendDetailsMenuHandler = friendDetailsMenu.gameObject.GetComponent<FriendDetailsMenuHandler>();

        Transform friendDetailsPanel = friendDetailsMenuHandler.friendDetailsPanel;
        Image avatar = friendDetailsPanel.GetComponentInChildren<Image>();
        TMP_Text playerDisplayName = friendDetailsPanel.GetComponentInChildren<TMP_Text>();

        friendDetailsMenuHandler.UserID = currentUserId;
        avatar.sprite = avatarImage.sprite;
        playerDisplayName.text = playerNameText.text;

        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendDetailsMenuCanvas);
    }

    public void SwitchView(PartyEntryView partyEntryView)
    {
        addMemberButton.gameObject.SetActive(partyEntryView == PartyEntryView.Empty);
        memberInfoPanel.gameObject.SetActive(partyEntryView == PartyEntryView.MemberInfo);
    }

    public void UpdateCurrentUserId(string userId)
    {
        currentUserId = userId;
    }

    public void UpdateMemberInfoUI(string playerName, Texture2D avatar = null)
    {
        playerNameText.text = playerName;
        if (avatar != null)
        {
            avatarImage.sprite = Sprite.Create(avatar, new Rect(0f, 0f, avatar.width, avatar.height), Vector2.zero);
        }

        SwitchView(PartyEntryView.MemberInfo);
    }

    public void ChangePanelColor(Color color)
    {
        Image entryPanelImage = this.GetComponent<Image>();
        entryPanelImage.color = color;

        playerAvatarPanelImage.color = color;
    }

    public void SetMemberInfoPanelInteractable(bool isInteractable)
    {
        memberInfoPanel.interactable = isInteractable;
    }
}
