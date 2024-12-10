// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;
using UnityEngine.UI;

public class MatchmakingSessionServerTypeSelection : MenuCanvas
{
    [SerializeField]
    private Button backButton;
    public static event Action OnBackButtonCalled;
    private InGameMode selectedGameMode = InGameMode.None;
    public InGameMode SelectedGameMode { get { return selectedGameMode; } }

    public void SetInGameMode(InGameMode inGameMode)
    {
        selectedGameMode = inGameMode;
    }

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
        OnBackButtonCalled?.Invoke();
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingSessionServerTypeSelection;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
