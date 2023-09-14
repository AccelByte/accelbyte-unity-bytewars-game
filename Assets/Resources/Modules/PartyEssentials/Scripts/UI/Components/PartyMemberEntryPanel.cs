using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberEntryPanel : MonoBehaviour
{
    public enum PartyEntryView
    {
        Empty,
        MemberInfo
    }
    private PartyEntryView _displayEntryView = PartyEntryView.Empty;

    [SerializeField] private Transform memberInfoPanel;
    [SerializeField] private Button addMemberButton;

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
