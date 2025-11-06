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

public class SentFriendRequestsMenu_Starter : MenuCanvas
{
    [Header("Friend Request Components")]
    [SerializeField] private GameObject friendEntryPrefab;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private RectTransform resultContentPanel;
    [SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();

    // TODO: Declare Module Wrappers here.

    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Load Outgoing Friend Requests here.
    }

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Define Module Wrapper listeners here.
    }

    private void OnDisable()
    {
        ClearFriendRequestList();
    }

    #region Add Friends Module

    private void LoadOutgoingFriendRequests()
    {
        // TODO: Implement Load Outgoing Friend Requests here.
        BytewarsLogger.LogWarning("The LoadOutgoingFriendRequests method is not implemented yet");
    }

    private void OnLoadOutgoingRequestsCompleted(Result<Friends> result)
    {
        // TODO: Implement Load Outgoing Friend Requests callback functions here.
        BytewarsLogger.LogWarning("The OnLoadOutgoingRequestsCompleted method is not implemented yet");
    }

    private void ClearFriendRequestList()
    {
        resultContentPanel.DestroyAllChildren();

        friendRequests.Clear();
    }

    // TODO: Implement Outgoing Friend Requests functions here.

    #endregion Add Friends Module

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SentFriendRequestsMenu_Starter;
    }
}
