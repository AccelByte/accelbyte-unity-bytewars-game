﻿// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class MultiplayerSettingsMenu : MenuCanvas
{
    [Header("Menu Components")]
    [SerializeField] private Button regionPreferencesButton;
    [SerializeField] private Button backButton;

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MultiplayerSettingsMenu;
    }

    private void Awake()
    {
        regionPreferencesButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.RegionPreferencesMenu);
        }));
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
}