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
    public enum PartyEntryView
    {
        Empty,
        MemberInfo
    }
    private PartyEntryView _displayEntryView = PartyEntryView.Empty;
    
    [SerializeField] private Button addMemberButton;
    [SerializeField] private Transform memberInfoPanel;
    [SerializeField] private Image playerAvatarPanelImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text playerNameText;

    private PartyEssentialsWrapper _partyWrapper;

    private const string PARTY_SESSION_TEMPLATE_NAME = "unity-party";
    
    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        
        addMemberButton.onClick.AddListener(InviteFriendsToParty);
    }

    public void SwitchView(PartyEntryView partyEntryView)
    {
        if (partyEntryView == PartyEntryView.Empty)
        {
            addMemberButton.gameObject.SetActive(true);
            memberInfoPanel.gameObject.SetActive(false);
        }
        else if (partyEntryView == PartyEntryView.MemberInfo)
        {
            memberInfoPanel.gameObject.SetActive(true);
            addMemberButton.gameObject.SetActive(false);
        }
    }

    public void UpdateMemberInfoUIs(string playerName, Result<Texture2D> avatar = null)
    {
        playerNameText.text = playerName;
        if (avatar != null)
        {
            avatarImage.sprite = Sprite.Create(avatar.Value, new Rect(0f, 0f, avatar.Value.width, avatar.Value.height), Vector2.zero);
        }
    }

    public void ChangePanelColor(Color color)
    {
        Image entryPanelImage = this.GetComponent<Image>();
        entryPanelImage.color = color;

        playerAvatarPanelImage.color = color;
    }
    
    private void InviteFriendsToParty()
    {
        if (string.IsNullOrEmpty(_partyWrapper.partyId))
        {
            _partyWrapper.CreateParty(PARTY_SESSION_TEMPLATE_NAME, OnCreatePartyCompleted);
        }
    }

    private void OnCreatePartyCompleted(Result<SessionV2PartySession> result)
    {
        Debug.Log("Successfully create a new party!");
        
        ChangeToFriendListMenu();
    }

    private void ChangeToFriendListMenu()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.FriendMenuCanvas);
    }
}
