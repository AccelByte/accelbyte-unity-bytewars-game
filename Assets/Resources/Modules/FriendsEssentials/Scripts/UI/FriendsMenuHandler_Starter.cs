// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendsMenuHandler_Starter : MenuCanvas
{
    [Header("Friends Component"), SerializeField] private GameObject friendEntryPrefab;
    
    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Result Column Panels"), SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;
    
    [Header("Menu Components"), SerializeField] private Button backButton;
    
    private readonly Dictionary<string, GameObject> friendEntries = new();
    
    private AssetEnum friendDetailMenuCanvas;
    
    private enum FriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }
    
    private FriendsView currentView = FriendsView.Default;
    
    private FriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FriendsView.Default);
            loadingPanel.gameObject.SetActive(value == FriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FriendsView.LoadSuccess);
            currentView = value;
        }
    }
    
    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Load Friends here.
    }

    private void Awake()
    {
        CurrentView = FriendsView.Default;
        friendDetailMenuCanvas = FriendsHelper.GetMenuByDependencyModule();
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Define Module Wrapper listeners here.

        ClearFriendList();
    }
    
    private void OnDisable()
    {
        ClearFriendList();
        
        CurrentView = FriendsView.Default;
    }

    #region Friend List Module

    #region Main Functions

    // TODO: Implement Friend List main functions here.

    #endregion Main Functions

    #region Callback Functions

    // TODO: Implement Friend List callback functions here.

    #endregion Callback Functions

    #region View Management

    private void ClearFriendList()
    {
        resultContentPanel.DestroyAllChildren();
        
        friendEntries.Clear();
    }
    
    private GameObject InstantiateToColumn(GameObject playerEntryPrefab)
    {
        bool leftPanelHasSpace = resultColumnLeftPanel.childCount <= resultColumnRightPanel.childCount;
        
        return Instantiate(playerEntryPrefab, leftPanelHasSpace ? resultColumnLeftPanel : resultColumnRightPanel);
    }

    // TODO: Implement Friend List view management functions here.

    #endregion View Management

    #endregion Friend List Module

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return GameObject.Find("HeaderPanel").gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendsMenuCanvas_Starter;
    }

    #endregion Menu Canvas Override
}
