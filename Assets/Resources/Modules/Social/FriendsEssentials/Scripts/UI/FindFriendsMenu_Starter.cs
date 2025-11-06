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

public class FindFriendsMenu_Starter : MenuCanvas
{
    [Header("Find Friends Components")]
    [SerializeField] private GameObject friendEntryPrefab;
    [SerializeField] private TMP_Text friendCode;
    [SerializeField] private Button friendCodeCopyButton;
    [SerializeField] private TMP_InputField friendSearchBar;
    [SerializeField] private Button friendSearchButton;

    [Header("Menu Components")]
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform resultContentPanel;
    [SerializeField] private Button backButton;

    private GameObject userResult;

    // TODO: Define module wrapper and status message map here.

    private void Awake()
    {
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
    }

    private void OnEnable()
    {
        friendSearchBar.enabled = true;
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
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
        buttonText.SetText(FriendsEssentialsModels.FriendCodeCopiedMessage);
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
        return AssetEnum.FindFriendsMenu_Starter;
    }

    #endregion MenuCanvas Override
}
