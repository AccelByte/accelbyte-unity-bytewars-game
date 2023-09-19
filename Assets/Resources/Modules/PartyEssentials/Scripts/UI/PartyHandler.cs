using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler : MenuCanvas
{
    [SerializeField] private PartyMemberEntryPanel[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;
    public static GameObject partyInvitationPrefab;

    private PartyEssentialsWrapper _partyWrapper;
    private Lobby _lobby;

    private const string DEFUSERNAME = "Player-";
    private Color _leaderPanelColor = Color.blue;

    // Start is called before the first frame update
    void Start()
    {
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);

        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();

        UpdateCurrentPlayerInfo();
        CheckPartyStatus();
    }

    private void UpdateCurrentPlayerInfo()
    {
        AuthEssentialsWrapper auth = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        if (auth.userData.display_name == "")
        {
            partyMemberEntryPanels[0].UpdateMemberInfoUIs(DEFUSERNAME + auth.userData.user_id.Substring(0, 5));
        }
        else
        {
            partyMemberEntryPanels[0].UpdateMemberInfoUIs(auth.userData.display_name);
        }

        partyMemberEntryPanels[0].ChangePanelColor(_leaderPanelColor);
    }

    private void CheckPartyStatus()
    {
        _partyWrapper.GetUserParties(OnGetUserPartiesCompleted);
    }

    private void OnGetUserPartiesCompleted(Result<PaginatedResponse<SessionV2PartySession>> result)
    {
        if (!result.IsError)
        {
            foreach (SessionV2PartySession partySession in result.Value.data)
            {
                _partyWrapper.LeaveParty(partySession.id, null);
            }
        }
    }

    private void OnLeaveButtonClicked()
    {
        _partyWrapper.LeaveParty(_partyWrapper.partyId, null);
    }

    public static void ReceivePartyInvitation(Result<SessionV2PartyInvitationNotification> invitation)
    {
        partyInvitationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PartyInvitationEntryPanel) as GameObject;
        PushNotificationHandler notificationHandler = MenuManager.Instance.GetChildComponent<PushNotificationHandler>();
        PartyInvitationEntryPanel invitationEntryPanel = notificationHandler.AddNotificationItem<PartyInvitationEntryPanel>(partyInvitationPrefab);
        invitationEntryPanel.UpdatePartyInvitationInfo(invitation.Value.partyId, invitation.Value.senderId);
    }
    
    public void OnPartyUpdated(Result<SessionV2PartySessionUpdatedNotification> result)
    {
        for(int index = 0; index < result.Value.members.Length; index++)
        {
            PartyMemberEntryPanel partyMemberEntryPanel = partyMemberEntryPanels[index].GetComponent<PartyMemberEntryPanel>();
            partyMemberEntryPanel.SwitchView(PartyMemberEntryPanel.PartyEntryView.MemberInfo);
            partyMemberEntryPanel.UpdateMemberInfoUIs(result.Value.members[index].id);
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
