using System;
using System.Collections.Generic;
using AccelByte.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler : MenuCanvas
{
    [SerializeField] private PartyMemberEntryPanel[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;

    private PartyEssentialsWrapper _partyWrapper;
    private AuthEssentialsWrapper _authWrapper;

    private readonly Color _leaderPanelColor = Color.blue;
    private readonly Color _memberPanelColor = Color.white;

    private const string DefaultDisplayName = "Player-";

    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        if (PartyHelper.CurrentPartyId == "")
        {
            DisplayOnlyCurrentPlayer();
        }
        else
        {
            DisplayPartyMembersData();
        }
    }

    private void OnEnable()
    {
        if (!String.IsNullOrEmpty(PartyHelper.CurrentPartyId) && PartyHelper.PartyMembersData.Count > 0 && _authWrapper)
        {
            DisplayPartyMembersData();
        }

        // please remove if button animation's onComplete changed.
        // current behavior: after leaving party, leave button will turn gray after reopening the party menu
        if (!leaveButton.interactable)
        {
            TMP_Text leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>();
            leaveButtonText.color = leaveButton.interactable ? Color.white : Color.gray;
        }
    }

    public void HandleNotInParty()
    {
        ResetPartyMemberEntryUI();
        DisplayOnlyCurrentPlayer();

        _partyWrapper.PartyId = "";
        PartyHelper.ResetPartyData();
    }

    private void ResetPartyMemberEntryUI()
    {
        foreach (PartyMemberEntryPanel entryPanel in partyMemberEntryPanels)
        {
            entryPanel.SwitchView(PartyEntryView.Empty);
            entryPanel.ChangePanelColor(_memberPanelColor);
            entryPanel.UpdateCurrentUserId("");
            entryPanel.SetMemberInfoPanelInteractable(false);
        }
    }

    private void DisplayOnlyCurrentPlayer()
    {
        string displayName = _authWrapper.userData.display_name;
        if (String.IsNullOrEmpty(displayName))
        {
            displayName = DefaultDisplayName + _authWrapper.userData.user_id.Substring(0, 5);
        }

        partyMemberEntryPanels[0].SwitchView(PartyEntryView.MemberInfo);
        partyMemberEntryPanels[0].UpdateMemberInfoUI(displayName);
        SetLeaveButtonInteractable(false);
    }

    public void DisplayPartyMembersData()
    {
        ResetPartyMemberEntryUI();
        for (int index = 0; index < PartyHelper.PartyMembersData.Count; index++)
        {
            PartyMemberData partyMemberData = PartyHelper.PartyMembersData[index];
            partyMemberEntryPanels[index].SwitchView(PartyEntryView.MemberInfo);
            partyMemberEntryPanels[index].UpdateCurrentUserId(partyMemberData.UserId);
            partyMemberEntryPanels[index].UpdateMemberInfoUI(partyMemberData.DisplayName, partyMemberData.Avatar);

            if (partyMemberData.UserId == PartyHelper.CurrentLeaderUserId)
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
        if (!String.IsNullOrEmpty(_partyWrapper.PartyId))
        {
            _partyWrapper.LeaveParty(_partyWrapper.PartyId, result => HandleNotInParty());

            PartyHelper partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
            partyHelper.TriggerMessageNotification("You left the party!");
        }
    }

    public void SetLeaveButtonInteractable(bool isInteractable)
    {
        leaveButton.interactable = isInteractable;

        TMP_Text leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>();
        leaveButtonText.color = isInteractable ? Color.white : Color.gray;
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
