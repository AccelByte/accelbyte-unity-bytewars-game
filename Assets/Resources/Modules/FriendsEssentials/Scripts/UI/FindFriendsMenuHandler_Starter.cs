// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FindFriendsMenuHandler_Starter : MenuCanvas
{
    [Header("Find Friends Components"), SerializeField] private GameObject friendEntryPrefab;
    [SerializeField] private TMP_Text friendCode;
    [SerializeField] private Button friendCodeCopyButton;
    [SerializeField] private TMP_InputField friendSearchBar;
    [SerializeField] private Button friendSearchButton;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private const string FriendCodeCopiedMessage = "Copied!";
    private const string FriendCodePreloadMessage = "...";

    private GameObject userResult;

    // TODO: Define module wrapper and status message map here.

    private enum FindFriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }

    private FindFriendsView currentView = FindFriendsView.Default;

    private FindFriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FindFriendsView.Default);
            loadingPanel.gameObject.SetActive(value == FindFriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FindFriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FindFriendsView.LoadSuccess);
            currentView = value;
        }
    }

    private void Awake()
    {
        CurrentView = FindFriendsView.Default;

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        friendCodeCopyButton.onClick.AddListener(OnFriendCodeCopyButtonClicked);
        friendSearchBar.onSubmit.AddListener(FindFriend);
        friendSearchButton.onClick.AddListener(() => FindFriend(friendSearchBar.text));
    }

    private void Start()
    {
        // TODO: Get Module Wrappers and Preload Friend Code here.
    }

    private void OnDisable()
    {
        ClearSearchPanel();

        CurrentView = FindFriendsView.Default;
    }

    #region Search for Players Module

    #region Main Functions

    private void FindFriend(string friendId)
    {
        BytewarsLogger.LogWarning("The FindFriend method is not implemented yet");
    }

    // TODO: Implement Search for Players main functions here.

    #endregion

    #region Callback Functions

    // TODO: Implement Search for Players callback functions here.

    #endregion Callback Functions

    #region View Management

    private void ClearSearchPanel()
    {
        friendSearchBar.text = string.Empty;

        resultContentPanel.DestroyAllChildren();

        if (userResult != null)
        {
            Destroy(userResult);
        }
    }

    private async void OnFriendCodeCopyButtonClicked()
    {
        GUIUtility.systemCopyBuffer = friendCode.text;
        TMP_Text buttonText = friendCodeCopyButton.GetComponentInChildren<TMP_Text>();

        string originalText = buttonText.text;
        buttonText.SetText(FriendCodeCopiedMessage);
        friendCodeCopyButton.interactable = false;

        await Task.Delay(TimeSpan.FromSeconds(2));

        buttonText.SetText(originalText);
        friendCodeCopyButton.interactable = true;
    }

    #endregion View Management

    #endregion Search for Players Module

    #region MenuCanvas Override

    public override GameObject GetFirstButton()
    {
        return friendSearchBar.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FindFriendsMenuCanvas_Starter;
    }

    #endregion MenuCanvas Override
}
