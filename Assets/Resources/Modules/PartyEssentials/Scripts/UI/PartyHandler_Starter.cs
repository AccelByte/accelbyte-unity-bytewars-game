using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler_Starter : MenuCanvas
{
    [SerializeField] private Transform[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject partyInvitationPrefab;

    // put your code here
    
    
    void Start()
    {
        // put your code here
    }
    
    // put your code here
    

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
