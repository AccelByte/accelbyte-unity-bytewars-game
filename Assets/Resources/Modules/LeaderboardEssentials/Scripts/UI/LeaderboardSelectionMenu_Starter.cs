using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSelectionMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        leaderboardListPanel.DestroyAllChildren();

        // Put your code here
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
        return AssetEnum.LeaderboardSelectionMenuCanvas_Starter;
    }
    
    private bool IsLeaderboardDataCached(LeaderboardDataV3[] newData)
    {
        string newDataSerialized = JsonConvert.SerializeObject(newData);
        
        // Put your code here

        return false;
    }
}