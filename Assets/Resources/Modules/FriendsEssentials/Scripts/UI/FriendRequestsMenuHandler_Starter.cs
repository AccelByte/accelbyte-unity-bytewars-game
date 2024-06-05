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

public class FriendRequestsMenuHandler_Starter : MenuCanvas
{
    [Header("Friend Request Components"), SerializeField] private GameObject friendEntryPrefab;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();

    // TODO: Declare Module Wrappers here.

    private enum FriendRequestsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }

    private FriendRequestsView currentView = FriendRequestsView.Default;

    private FriendRequestsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FriendRequestsView.Default);
            loadingPanel.gameObject.SetActive(value == FriendRequestsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FriendRequestsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FriendRequestsView.LoadSuccess);
            currentView = value;
        }
    }

    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Load Incoming Friend Requests here.
    }

    private void Awake()
    {
        CurrentView = FriendRequestsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Define Module Wrapper listeners here.
    }

    private void OnDisable()
    {
        ClearFriendRequestList();
    }

    #region Add Friends Module

    #region Main Functions

    // TODO: Implement Incoming Friend Requests main functions here.

    #endregion Main Functions

    #region Callback Functions

    // TODO: Implement Incoming Friend Requests callback functions here.

    #endregion Callback Functions

    #region View Management

    private void ClearFriendRequestList()
    {
        resultContentPanel.DestroyAllChildren();

        friendRequests.Clear();
    }

    // TODO: Implement Incoming Friend Requests view management here.

    #endregion View Management

    #endregion Add Friends Module

    public override GameObject GetFirstButton()
    {
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendRequestsMenuCanvas_Starter;
    }
}
