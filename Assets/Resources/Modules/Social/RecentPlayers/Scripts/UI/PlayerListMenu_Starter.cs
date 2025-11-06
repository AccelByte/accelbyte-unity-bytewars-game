// Copyright (c) 2025 - 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Core;
using Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerListMenu_Starter : MenuCanvas
{
    [Header("Player Component")]
    [SerializeField] private GameObject playerEntryPrefab;
    
    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> playerEntries = new();

    // TODO: Declare Module Wrappers here.

    private void OnEnable()
    {
        // TODO: Define Module Wrappers here.
        
        if (GameManager.Instance.InGamePause != null)
        {
            backButton.onClick.AddListener(GameManager.Instance.InGamePause.BackToPreviousPauseMenu);
        }
        LoadPlayerList();
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveAllListeners();
    }
    
    #region Player List Module

    private void LoadPlayerList()
    {
        // TODO: Implement Load Player List function here.
        BytewarsLogger.LogWarning("LoadPlayerList is not yet implemented.");
    }
    
    private void OnQueryPlayerListCompleted (Result<List<RecentPlayerInfo>> result)
    {
        // TODO: Implement OnQueryPlayerListCompleted function here.
        BytewarsLogger.LogWarning("OnQueryPlayerListCompleted is not yet implemented.");
    }

    // TODO: Implement Player List functions here.
    
    #endregion Player List Module

    private void ClearEntries()
    {
        resultColumnLeftPanel.DestroyAllChildren();
        resultColumnRightPanel.DestroyAllChildren();
        playerEntries.Clear();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PlayerListMenu;
    }
}
