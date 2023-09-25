using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PartyMemberEntryPanel : MonoBehaviour
{
    private PartyEntryView _displayEntryView = PartyEntryView.Empty;
    
    [SerializeField] private Button addMemberButton;
    [SerializeField] private Button memberInfoPanel;
    [SerializeField] private Image playerAvatarPanelImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text playerNameText;

    private string _currentUserId;
    
    // Start is called before the first frame update
    void Start()
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

        friendDetailsMenuHandler.UserID = _currentUserId;
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
        _currentUserId = userId;
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
