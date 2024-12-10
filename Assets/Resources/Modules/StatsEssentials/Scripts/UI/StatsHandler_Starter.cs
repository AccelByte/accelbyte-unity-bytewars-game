using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsHandler_Starter : MenuCanvas
{
    [SerializeField] private TMP_Text singlePlayerStatValueText;
    [SerializeField] private TMP_Text eliminationStatValueText;
    [SerializeField] private TMP_Text teamDeathmatchStatValueText;
    [SerializeField] private Button backButton;

    //Paste StatCodes names const variables from "Put It All Together" (step number 3) here
    
    
    //Paste StatsEssentialsWrapper_Starter declaration from "Put It All Together" (step number 2) here
    
    
    void Start()
    {
        //Paste Get Stats Wrapper from "Put It All Together" (step number 2) here
        
        
        //Paste UI initialization from "Add a Stats Profile Menu" (step number 4) here
        
    }

    //Paste OnEnable() function from "Put It All Together" (step number 7) here
    
    
    //Paste DisplayStats() function from unit "Add a Stats Profile Menu" (step number 3) here
    
    
    //Paste OnGetUserStatsCompleted() function from "Put It All Together" (step number 4) here
    
    
    private void OnBackButtonClicked(){
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.StatsProfileMenuCanvas_Starter;
    }
}
