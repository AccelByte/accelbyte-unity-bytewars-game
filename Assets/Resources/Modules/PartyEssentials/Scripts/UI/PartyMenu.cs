// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyMenu : MenuCanvas
{
    [SerializeField] private Transform memberEntryContainer;
    [SerializeField] private PartyMemberEntry memberEntryPrefab;

    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;

    private PartyEssentialsWrapper partyWrapper;

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenu;
    }

    public override GameObject GetFirstButton()
    {
        return leaveButton.gameObject;
    }

    private void OnEnable()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        leaveButton.onClick.AddListener(LeaveParty);
        widgetSwitcher.OnRetryButtonClicked += DisplayParty;

        partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        if (partyWrapper) 
        {
            partyWrapper.OnPartyUpdateDelegate += DisplayParty;
            DisplayParty();
        }
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveAllListeners();
        leaveButton.onClick.RemoveAllListeners();
        widgetSwitcher.OnRetryButtonClicked -= DisplayParty;

        if (partyWrapper) 
        {
            partyWrapper.OnPartyUpdateDelegate -= DisplayParty;
        }
    }

    private void DisplayParty() 
    {
        if (!partyWrapper) 
        {
            BytewarsLogger.LogWarning("Failed to display party. Party wrapper is null.");
            return;
        }

        // Abort if no user is logged-in.
        PlayerState currentUser = GameData.CachedPlayerState;
        if (currentUser == null) 
        {
            BytewarsLogger.LogWarning("Failed to display party. There is no current user logged-in.");
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        // Immediately clean the old party member list.
        int oldEntriesCount = memberEntryContainer.childCount;
        for (int i = 0; i < oldEntriesCount; i++) 
        {
            Transform oldEntry = memberEntryContainer.GetChild(0).transform;
            oldEntry.SetParent(null);
            DestroyImmediate(oldEntry.gameObject);
        }

        // Display party details.
        partyWrapper.GetPartyDetails((Result<PartyEssentialsModels.PartyDetailsModel> result) => 
        {
            // Instantiate party members.
            if (!result.IsError) 
            {
                foreach (AccountUserPlatformData memberInfo in result.Value.MemberUserInfos)
                {
                    bool isLeader = memberInfo.UserId == result.Value.PartySession.leaderId;
                    PartyMemberEntry memberEntry = Instantiate(memberEntryPrefab, memberEntryContainer);
                    memberEntry.SetPartyMember(memberInfo, isLeader);
                }
            }
            /* If failed to get party details because the player is not in any party.
             * Then, display current logged-in player entry only.*/
            else if (partyWrapper.CurrentPartySession == null) 
            {
                PartyMemberEntry memberEntry = Instantiate(memberEntryPrefab, memberEntryContainer);
                memberEntry.SetPartyMember(new AccountUserPlatformData
                {
                    UserId = currentUser.PlayerId,
                    DisplayName = currentUser.PlayerName,
                    AvatarUrl = currentUser.AvatarUrl
                }, true);
            }
            // Else, display error message.
            else 
            {
                widgetSwitcher.ErrorMessage = "Failed to load party. An error occurred.";
                widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                return;
            }

            // Display empty entries to add a new party member.
            int maxMembers = PartyEssentialsModels.PartyMaxMembers;
            int maxEmptyEntries = Mathf.Clamp(maxMembers - memberEntryContainer.childCount, 0, maxMembers);
            for (int i = 0; i < maxEmptyEntries; i++)
            {
                PartyMemberEntry memberEntry = Instantiate(memberEntryPrefab, memberEntryContainer);
                memberEntry.ResetPartyMember();
            }

            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
        });
    }

    private void LeaveParty() 
    {
        // If not in party, simply close the menu.
        if (partyWrapper.CurrentPartySession == null) 
        {
            backButton.onClick.Invoke();
            return;
        }

        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        partyWrapper.LeaveParty((Result result) =>
        {
            if (result.IsError) 
            {
                widgetSwitcher.ErrorMessage = "Failed to leave party. An error occurred.";
                widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                return;
            }

            backButton.onClick.Invoke();
        });
    }
}
