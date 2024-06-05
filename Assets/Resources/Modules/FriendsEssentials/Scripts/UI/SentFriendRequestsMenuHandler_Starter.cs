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

public class SentFriendRequestsMenuHandler_Starter : MenuCanvas
{
    [Header("Friend Request Components"), SerializeField] private GameObject friendEntryPrefab;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;
    
    [Header("Menu Components"), SerializeField] private Button backButton;

    private readonly Dictionary<string, GameObject> friendRequests = new();

    // TODO: Declare Module Wrappers here.

    private enum SentFriendRequestsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }

    private SentFriendRequestsView currentView = SentFriendRequestsView.Default;

    private SentFriendRequestsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == SentFriendRequestsView.Default);
            loadingPanel.gameObject.SetActive(value == SentFriendRequestsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == SentFriendRequestsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == SentFriendRequestsView.LoadSuccess);
            currentView = value;
        }
    }

    private void OnEnable()
    {
        // TODO: Define Module Wrappers and Load Outgoing Friend Requests here.
    }

    private void Awake()
    {
        CurrentView = SentFriendRequestsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        ClearFriendRequestList();
    }

    private void OnDisable()
    {
        ClearFriendRequestList();
    }

    #region Add Friends Module

    #region Main Functions

    // TODO: Implement Outgoing Friend Requests main functions here.

    #endregion Main Functions

    #region Callback Functions

    // TODO: Implement Outgoing Friend Requests callback functions here.

    #endregion Callback Functions

    #region View Management

    private void ClearFriendRequestList()
    {
        resultContentPanel.DestroyAllChildren();

        friendRequests.Clear();
    }

    // TODO: Implement Outgoing Friend Requests view management here.

    #endregion View Management

    #endregion Add Friends Module

    public override GameObject GetFirstButton()
    {
        return defaultPanel.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SentFriendRequestsMenuCanvas_Starter;
    }
}
