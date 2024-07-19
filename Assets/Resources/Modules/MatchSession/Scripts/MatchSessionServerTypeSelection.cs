// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;
using UnityEngine.UI;

public class MatchSessionServerTypeSelection : MenuCanvas
{
    public static event Action OnBackButtonCalled;
    public InGameMode SelectedGameMode { get { return selectedGameMode; } }
    private InGameMode selectedGameMode = InGameMode.None;
    [SerializeField]
    private Button backButton;

    public void SetInGameMode(InGameMode inGameMode)
    {
        selectedGameMode = inGameMode;
    }

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnDestroy()
    {
        backButton.onClick.RemoveAllListeners();
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
        OnBackButtonCalled?.Invoke();
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchSessionServerTypeSelection;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
