// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class OnlineSettingsMenu : MenuCanvas
{
    [Header("Menu Components")]
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button backButton;

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.OnlineSettingsMenu;
    }

    private void Awake()
    {
        multiplayerButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.MultiplayerSettingsMenu);
        }));
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
}
