// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
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

        LoadFriendList();
    }

    private void Awake()
    {
        CurrentView = FriendsView.Default;
        friendDetailMenuCanvas = FriendsHelper.GetMenuByDependencyModule();
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Define Module Wrapper listeners here.

        ClearFriendList();
    }

    private void OnDestroy()
    {
        // TODO: Unbind event action here
    }

    private void OnDisable()
    {
        ClearFriendList();
    }

    #region Friend List Module

    private void LoadFriendList()
    {
        // TODO: Implement Load Friend List function here.
        BytewarsLogger.LogWarning("The LoadFriendList method is not implemented yet");
    }

    private void OnLoadFriendListCompleted(Result<Friends> result)
    {
        // TODO: Implement Load Friend List callback functions here.
        BytewarsLogger.LogWarning("The OnLoadFriendListCompleted method is not implemented yet");
    }

    private void ClearFriendList()
    {
        resultColumnLeftPanel.DestroyAllChildren();
        resultColumnRightPanel.DestroyAllChildren();
        
        friendEntries.Clear();
    }
    
    private GameObject InstantiateToColumn(GameObject playerEntryPrefab)
    {
        bool shouldPlaceOnRightPanel = resultColumnLeftPanel.childCount > resultColumnRightPanel.childCount;
        
        return Instantiate(playerEntryPrefab, shouldPlaceOnRightPanel ? resultColumnRightPanel : resultColumnLeftPanel);
    }

    // TODO: Implement Friend List functions here.

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
