using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
    private string[] membersUserId;
    private Dictionary<string, string> memberDatas;
    
    private const string DEFUSERNAME = "Player-";
    private Color _leaderPanelColor = Color.blue;

    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();

        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
        
        DisplayOnlyCurrentPlayer();
    }

    private void OnEnable()
    {
        if (!currentPartyId.IsNullOrEmpty())
        {
            DisplayPartyMembersData();
        }
        else
        {
            ResetPartyMemberEntryUI();
        }
    }
    
    public void ResetPartyMemberEntryUI()
    {
        foreach (PartyMemberEntryPanel entryPanel in partyMemberEntryPanels)
        {
            entryPanel.SwitchView(PartyEntryView.Empty);
            entryPanel.ChangePanelColor(Color.white);
        }
        DisplayOnlyCurrentPlayer();
    }
    
    private void DisplayOnlyCurrentPlayer()
    {
        string displayName = _authWrapper.userData.display_name;
        if (displayName.IsNullOrEmpty())
        {
            displayName = DEFUSERNAME + _authWrapper.userData.user_id.Substring(0, 5);
        }

        partyMemberEntryPanels[0].SwitchView(PartyEntryView.MemberInfo);
        partyMemberEntryPanels[0].UpdateMemberInfoUIs(displayName);
        SetLeaveButtonInteractable(false);
    }

    public void UpdatePartyMembersData(SessionV2MemberData[] members, string leaderId = null)
    {
        Debug.Log($"[PARTY] UpdatePartyMembersData {members.Length} + {leaderId}");
        // set current party's leader id
        if (currentLeaderUserId == "" && leaderId != "")
        {
            currentLeaderUserId = leaderId;
        }
        
        // get members' user info data
        membersUserId = members.Select(member => member.id).ToArray();
        _authWrapper.BulkGetUserInfo(membersUserId, result =>
        {
            if (!result.IsError)
            {
                memberDatas = new Dictionary<string, string>();
                foreach (BaseUserInfo userData in result.Value.data)
                {
                    string displayName = userData.displayName == "" ? DEFUSERNAME + userData.userId.Substring(0, 5) : userData.displayName;
                    memberDatas.Add(userData.userId, displayName);
                }

                if (gameObject.activeSelf)
                {
                    DisplayPartyMembersData();
                }
            }
        });
    }

    public void DisplayPartyMembersData()
    {
        Debug.Log($"[PARTY] DisplayPartyMembersData {membersUserId.Length} + {currentLeaderUserId}");
        for (int index = 0; index < membersUserId.Length; index++)
        {
            string userId = membersUserId[index];
            var currentIndex = index;
            Debug.Log($"[PARTY] Looping 1.. {userId} | {memberDatas[userId]}");
            
            _authWrapper.GetUserAvatar(userId, avatarResult =>
            {
                Debug.Log($"[PARTY] Looping 2.. {userId} || {memberDatas[userId]}");
                if (!avatarResult.IsError)
                {
                    partyMemberEntryPanels[currentIndex].UpdateMemberInfoUIs(memberDatas[userId], avatarResult);
                }
                else
                {
                    partyMemberEntryPanels[currentIndex].UpdateMemberInfoUIs(memberDatas[userId]);
                }

                // change panel color if leader
                if (userId == currentLeaderUserId)
                {
                    partyMemberEntryPanels[currentIndex].ChangePanelColor(_leaderPanelColor);
                } 
            });
        }
    }

    private void OnLeaveButtonClicked()
    {
        if (!_partyWrapper.partyId.IsNullOrEmpty())
        {
            _partyWrapper.LeaveParty(_partyWrapper.partyId, result =>
            {
                currentPartyId = "";
                currentLeaderUserId = "";
                ResetPartyMemberEntryUI();
            });
        }
    }
    
    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public void SetLeaveButtonInteractable(bool isInteractable)
    {
        leaveButton.interactable = isInteractable;
        TMP_Text leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>();
        leaveButtonText.color = isInteractable ? Color.white : Color.grey;
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
