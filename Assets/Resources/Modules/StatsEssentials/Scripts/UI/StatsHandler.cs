// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsHandler : MenuCanvas
{
    [SerializeField] private TMP_Text singlePlayerStatValueText;
    [SerializeField] private TMP_Text eliminationStatValueText;
    [SerializeField] private TMP_Text teamDeathmatchStatValueText;
    [SerializeField] private Button backButton;

    // Statcodes' name configured in Admin Portal
    private const string SinglePlayerStatCode = "unity-highestscore-singleplayer";
    private const string EliminationStatCode = "unity-highestscore-elimination";
    private const string TeamDeathmatchStatCode = "unity-highestscore-teamdeathmatch";
	
    private StatsEssentialsWrapper statsWrapper;
    
    // Start is called before the first frame update
    void Start()
    {
        // Get stats' wrapper
        statsWrapper = TutorialModuleManager.Instance.GetModuleClass<StatsEssentialsWrapper>();

        // UI initialization
        backButton.onClick.AddListener(OnBackButtonClicked);
		
        // Set default values
        singlePlayerStatValueText.text = "0";
        eliminationStatValueText.text = "0";
        teamDeathmatchStatValueText.text = "0";
        
        DisplayStats();
    }

    void OnEnable()
    {
        if (gameObject.activeSelf && statsWrapper != null)
        {
            DisplayStats();
        }
    }

    private void DisplayStats()
    {
        // Trying to get the stats values
        string[] statCodes =
        {
            SinglePlayerStatCode,
            EliminationStatCode,
            TeamDeathmatchStatCode
        };
        statsWrapper.GetUserStatsFromClient(statCodes, null, OnGetUserStatsCompleted);
    }

    private void OnGetUserStatsCompleted(Result<PagedStatItems> result)
    {
        if (!result.IsError){
            foreach (StatItem statItem in result.Value.data)
            {
                BytewarsLogger.Log("[STATS]" + statItem.statCode + " - " + statItem.value);
                switch (statItem.statCode)
                {
                    case SinglePlayerStatCode:
                        singlePlayerStatValueText.text = statItem.value.ToString();
                        break;
                    case EliminationStatCode:
                        eliminationStatValueText.text = statItem.value.ToString();
                        break;
                    case TeamDeathmatchStatCode:
                        teamDeathmatchStatValueText.text = statItem.value.ToString();
                        break;
                }
            }
        }
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.StatsProfileMenuCanvas;
    }
}
