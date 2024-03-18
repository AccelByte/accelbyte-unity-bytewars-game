using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform rankingListPanel;
    [SerializeField] private Transform placeholderText;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject rankingEntryPanelPrefab;
    [SerializeField] private RankingEntryPanel userRankingPanel;

    #region "Tutorial implementation"
    // Put your code here
    #endregion

    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        // Put your code here
    }

    private void OnDisable()
    {
        placeholderText.gameObject.SetActive(true);
        rankingListPanel.DestroyAllChildren(placeholderText);
        userRankingPanel.ResetRankingEntry();
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
        return AssetEnum.LeaderboardMenuCanvas_Starter;
    }
}