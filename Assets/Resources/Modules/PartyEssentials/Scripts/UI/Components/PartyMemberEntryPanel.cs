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
    [SerializeField] private Transform memberInfoPanel;
    [SerializeField] private Image playerAvatarPanelImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text playerNameText;

    // Start is called before the first frame update
    void Start()
    {
        addMemberButton.onClick.AddListener(OnAddMemberButtonClicked);
    }

    public void SwitchView(PartyEntryView partyEntryView)
    {
        addMemberButton.gameObject.SetActive(partyEntryView == PartyEntryView.Empty);
        memberInfoPanel.gameObject.SetActive(partyEntryView == PartyEntryView.MemberInfo);
    }

    public void UpdateMemberInfoUIs(string playerName, Result<Texture2D> avatar = null)
    {
        playerNameText.text = playerName;
        if (avatar != null)
        {
            avatarImage.sprite = Sprite.Create(avatar.Value, new Rect(0f, 0f, avatar.Value.width, avatar.Value.height), Vector2.zero);
        }
        
        SwitchView(PartyEntryView.MemberInfo);
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

    private void OnAddMemberButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendMenuCanvas);
    }
}
