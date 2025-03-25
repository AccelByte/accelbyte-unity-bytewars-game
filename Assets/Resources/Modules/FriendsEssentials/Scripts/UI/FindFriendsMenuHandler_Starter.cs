// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using Cysharp.Threading.Tasks;
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
        friendSearchBar.text = string.Empty;

        CurrentView = FindFriendsView.Default;
    }

    private void OnEnable()
    {
        friendSearchBar.enabled = true;
    }

    #region Search for Players Module

    private void FindFriend(string friendId)
    {
        BytewarsLogger.LogWarning("The FindFriend method is not implemented yet");
    }

    private void ClearSearchPanel()
    {
        resultContentPanel.DestroyAllChildren();

        if (userResult != null)
        {
            Destroy(userResult);
        }
    }

    private async void OnFriendCodeCopyButtonClicked()
    {
        AccelByteWarsUtility.CopyToClipboard(friendCode.text);
        TMP_Text buttonText = friendCodeCopyButton.GetComponentInChildren<TMP_Text>();

        string originalText = buttonText.text;
        buttonText.SetText(FriendsHelper.FriendCodeCopiedMessage);
        friendCodeCopyButton.interactable = false;

        await UniTask.Delay(TimeSpan.FromSeconds(2));

        buttonText.SetText(originalText);
        friendCodeCopyButton.interactable = true;
    }

    // TODO: Implement Search for Players functions here.

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
