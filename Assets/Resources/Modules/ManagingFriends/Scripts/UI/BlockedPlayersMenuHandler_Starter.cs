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

public class BlockedPlayersMenuHandler_Starter : MenuCanvas
{
    [Header("Blocked Players Component"), SerializeField] private GameObject playerEntryPrefab;
    
    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;
    
    [Header("Menu Components"), SerializeField] private Button backButton;
    
    private readonly Dictionary<string, GameObject> blockedPlayers = new();
    
    private enum BlockedFriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private BlockedFriendsView currentView = BlockedFriendsView.Default;
    
    private BlockedFriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == BlockedFriendsView.Default);
            loadingPanel.gameObject.SetActive(value == BlockedFriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == BlockedFriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == BlockedFriendsView.LoadSuccess);
            currentView = value;
        }
    }
    
    private void OnEnable()
    {
        // TODO: Get Module Wrappers and load blocked players here.
    }

    private void Awake()
    {
        CurrentView = BlockedFriendsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }
    
    private void OnDisable()
    {
        ClearBlockedPlayers();
        
        CurrentView = BlockedFriendsView.Default;
    }

    #region Managing Friends Module

    #region Main Functions

    // TODO: Implement Block Player main functions here.

    #endregion Main Functions

    #region Callback Functions

    // TODO: Implement Block Player callback functions here.

    #endregion Callback Functions

    #region View Management

    private void ClearBlockedPlayers()
    {
        resultContentPanel.DestroyAllChildren();
        
        blockedPlayers.Clear();
    }

    // TODO: Implement Block Player view management functions here.

    #endregion View Management

    #endregion Managing Friends Module

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BlockedPlayersMenuCanvas_Starter;
    }

    #endregion Menu Canvas Override
}
