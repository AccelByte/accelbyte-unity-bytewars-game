using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
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

        // Put your code here
    }

    private void OnDisable()
    {
        leaderboardListPanel.DestroyAllChildren();
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
}