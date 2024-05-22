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

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;

    private const string FriendCodeCopiedMessage = "Copied!";
    private const string FriendCodePreloadMessage = "...";

    private GameObject userResult;

    //copy from Putting It All Together step 1

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

        backButton.onClick.AddListener(OnBackButtonClicked);
        friendCodeCopyButton.onClick.AddListener(OnFriendCodeCopyButtonClicked);
        //copy from Ready The UI step 1
    }

    private void Start()
    {
        //copy from Putting It All Together step 2
    }

    private void OnDisable()
    {
        ClearSearchPanel();

        CurrentView = FindFriendsView.Default;
    }

    #region ButtonAction

    private static void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
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

    #endregion

    #region Search for Players Module

    #region View Management

    /// <summary>
    /// Clear results in search panel
    /// </summary>
    private void ClearSearchPanel()
    {
        friendSearchBar.text = string.Empty;

        resultContentPanel.DestroyAllChildren();

        if (userResult != null)
        {
            Destroy(userResult);
        }
    }

    #endregion View Management

    #endregion Search for Players Module

    #region MenuCanvasOverride

    public override GameObject GetFirstButton()
    {
        return friendSearchBar.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FindFriendsMenuCanvas_Starter;
    }

    #endregion

}
