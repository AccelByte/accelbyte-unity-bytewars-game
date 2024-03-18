using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCycleMenu_Starter : MenuCanvas
{
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button backButton;

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Put your code here
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    public override GameObject GetFirstButton()
    {
        return allTimeButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardCycleMenuCanvas_Starter;
    }
}