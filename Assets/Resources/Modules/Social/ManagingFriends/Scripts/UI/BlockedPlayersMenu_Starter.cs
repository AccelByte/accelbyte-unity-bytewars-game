// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class BlockedPlayersMenu_Starter : MenuCanvas
{
    [Header("Blocked Players Component")]
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> blockedPlayers = new();

    // TODO: Declare Module Wrappers here.
        
    private void OnEnable()
    {
        // TODO: Get Module Wrappers and load blocked players here.
    }

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        ClearBlockedPlayers();
    }
    
    private void OnDisable()
    {
        ClearBlockedPlayers();
    }

    #region Managing Friends Module

    private void LoadBlockedPlayers()
    {
        // TODO: Implement Load Blocked Players function here.
        BytewarsLogger.LogWarning("The LoadBlockedPlayers method is not implemented yet");
    }

    private void OnLoadBlockedPlayersCompleted(Result<BlockedList> result)
    {
        // TODO: Implement Load Blocked Players callback functions here.
        BytewarsLogger.LogWarning("The OnLoadBlockedPlayersCompleted method is not implemented yet");
    }

    private void ClearBlockedPlayers()
    {
        resultContentPanel.DestroyAllChildren();
        
        blockedPlayers.Clear();
    }

    // TODO: Implement Block Player functions here.

    #endregion Managing Friends Module

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BlockedPlayersMenu_Starter;
    }

    #endregion Menu Canvas Override
}
