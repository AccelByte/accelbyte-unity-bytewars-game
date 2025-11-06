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

public class FriendsMenu_Starter : MenuCanvas
{
    [Header("Friends Component")]
    [SerializeField] private GameObject friendEntryPrefab;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendEntries = new();
    
    private AssetEnum friendDetailsAssetEnum;
    
    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Load Friends here.

        LoadFriendList();
    }

    private void Awake()
    {
        friendDetailsAssetEnum = FriendsEssentialsModels.GetMenuByDependencyModule();
        
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
        return AssetEnum.FriendsMenu_Starter;
    }

    #endregion Menu Canvas Override
}
