using System.Collections.Generic;
using AccelByte.Core;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class PartyHandler : MenuCanvas
{
    [SerializeField] private PartyMemberEntryPanel[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;

    private PartyEssentialsWrapper _partyWrapper;
    private AuthEssentialsWrapper _authWrapper;

    [HideInInspector] public string currentPartyId = "";
    [HideInInspector] public string currentLeaderUserId = "";
    public List<PartyMemberData> MembersUserInfo = new List<PartyMemberData>();
    
    private const string DEFAULT_DISPLAY_NAME = "Player-";
    private Color _leaderPanelColor = Color.blue;
    private Color _memberPanelColor = Color.white;

    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        if (currentPartyId == "")
        {
            DisplayOnlyCurrentPlayer();
        }
        else
        {
            DisplayPartyMembersInfo();
        }
    }

    private void OnEnable()
    {
        if (!currentPartyId.IsNullOrEmpty() && MembersUserInfo.Count > 0 && _authWrapper)
        {
            DisplayPartyMembersInfo();
        }
    }
    
    public void SetLeaveButtonInteractable(bool isInteractable)
    {
        leaveButton.interactable = isInteractable;
    }
    
    public void HandleNotInParty()
    {
        ResetPartyMemberEntryUI();
        DisplayOnlyCurrentPlayer();

        _partyWrapper.partyId = "";
        currentPartyId = "";
        currentLeaderUserId = "";
    }
    
    public void ResetPartyMemberEntryUI()
    {
        foreach (PartyMemberEntryPanel entryPanel in partyMemberEntryPanels)
        {
            entryPanel.SwitchView(PartyEntryView.Empty);
            entryPanel.ChangePanelColor(_memberPanelColor);
            entryPanel.UpdateCurrentUserId("");
            entryPanel.SetMemberInfoPanelInteractable(false);
        }
    }
    
    public void DisplayOnlyCurrentPlayer()
    {
        string displayName = _authWrapper.userData.display_name;
        if (displayName.IsNullOrEmpty())
        {
            displayName = DEFAULT_DISPLAY_NAME + _authWrapper.userData.user_id.Substring(0, 5);
        }

        partyMemberEntryPanels[0].SwitchView(PartyEntryView.MemberInfo);
        partyMemberEntryPanels[0].UpdateMemberInfoUI(displayName);
        SetLeaveButtonInteractable(false);
    }

    public void DisplayPartyMembersInfo()
    {
        ResetPartyMemberEntryUI();
        for (int index = 0; index < MembersUserInfo.Count; index++)
        {
            PartyMemberData partyMemberData = MembersUserInfo[index];
            partyMemberEntryPanels[index].SwitchView(PartyEntryView.MemberInfo);
            partyMemberEntryPanels[index].UpdateCurrentUserId(partyMemberData.UserId);
            partyMemberEntryPanels[index].UpdateMemberInfoUI(partyMemberData.DisplayName, partyMemberData.Avatar);

            if (partyMemberData.UserId == currentLeaderUserId)
            {
                partyMemberEntryPanels[index].ChangePanelColor(_leaderPanelColor);
            }
            
            // Set MemberInfoPanel to be interactable only to other players' MemberInfoPanel
            bool isOtherPanel = _authWrapper.userData.user_id != partyMemberData.UserId;
            partyMemberEntryPanels[index].SetMemberInfoPanelInteractable(isOtherPanel);
        }
    }

    private void OnLeaveButtonClicked()
    {
        if (!_partyWrapper.partyId.IsNullOrEmpty())
        {
            _partyWrapper.LeaveParty(_partyWrapper.partyId, result => HandleNotInParty());
            
            PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
            partyHelper.TriggerMessageNotification(("You left the party!"));
        }
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return leaveButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenuCanvas;
    }
}
