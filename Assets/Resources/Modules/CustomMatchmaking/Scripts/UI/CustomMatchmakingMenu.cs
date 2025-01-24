// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;
using static AccelByteWarsWidgetSwitcher;

public class CustomMatchmakingMenu : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Button startMatchmakingButton;
    [SerializeField] private Button backButton;

    private CustomMatchmakingWrapper customMatchmakingWrapper;

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CustomMatchmakingMenu;
    }

    public override GameObject GetFirstButton()
    {
        return startMatchmakingButton.gameObject;
    }

    private void OnEnable()
    {
        customMatchmakingWrapper = TutorialModuleManager.Instance.GetModuleClass<CustomMatchmakingWrapper>();

        startMatchmakingButton.onClick.AddListener(StartMatchmaking);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        widgetSwitcher.OnRetryButtonClicked += StartMatchmaking;
        widgetSwitcher.OnCancelButtonClicked += CancelMatchmaking;
        widgetSwitcher.OnStateChanged += OnSwitcherStateChanged;
        widgetSwitcher.SetWidgetState(WidgetState.Not_Empty);

        if (customMatchmakingWrapper)
        {
            customMatchmakingWrapper.OnMatchmakingStarted += OnMatchmakingStarted;
            customMatchmakingWrapper.OnMatchmakingStopped += OnMatchmakingStopped;
            customMatchmakingWrapper.OnMatchmakingPayload += OnMatchmakingPayload;
            customMatchmakingWrapper.OnMatchmakingError += OnMatchmakingError;
        }
    }

    private void OnDisable()
    {
        startMatchmakingButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();

        widgetSwitcher.OnRetryButtonClicked -= StartMatchmaking;
        widgetSwitcher.OnCancelButtonClicked -= CancelMatchmaking;
        widgetSwitcher.OnStateChanged -= OnSwitcherStateChanged;

        if (customMatchmakingWrapper) 
        {
            customMatchmakingWrapper.OnMatchmakingStarted -= OnMatchmakingStarted;
            customMatchmakingWrapper.OnMatchmakingStopped -= OnMatchmakingStopped;
            customMatchmakingWrapper.OnMatchmakingPayload -= OnMatchmakingPayload;
            customMatchmakingWrapper.OnMatchmakingError -= OnMatchmakingError;
        }
    }

    private void StartMatchmaking() 
    {
        widgetSwitcher.LoadingMessage = CustomMatchmakingModels.RequestMatchmakingMessage;
        widgetSwitcher.EnableCancelButton(false);
        widgetSwitcher.SetWidgetState(WidgetState.Loading);

        customMatchmakingWrapper.StartMatchmaking();
    }

    private void CancelMatchmaking() 
    {
        widgetSwitcher.LoadingMessage = CustomMatchmakingModels.CancelMatchmakingMessage;
        widgetSwitcher.EnableCancelButton(false);
        widgetSwitcher.SetWidgetState(WidgetState.Loading);

        customMatchmakingWrapper.CancelMatchmaking();
    }

    private void OnMatchmakingStarted() 
    {
        widgetSwitcher.LoadingMessage = CustomMatchmakingModels.FindingMatchMessage;
        widgetSwitcher.EnableCancelButton(true);
        widgetSwitcher.SetWidgetState(WidgetState.Loading);
    }

    private void OnMatchmakingStopped(WebSocketCloseCode closeCode)
    {
        if (closeCode == WebSocketCloseCode.Normal) 
        {
            widgetSwitcher.SetWidgetState(WidgetState.Not_Empty);
        }
        else
        {
            // Display generic error message.
            OnMatchmakingError(CustomMatchmakingModels.MatchmakingErrorMessage);
        }
    }

    private void OnMatchmakingPayload(CustomMatchmakingModels.MatchmakerPayload payload)
    {
        widgetSwitcher.LoadingMessage = payload.message;
        widgetSwitcher.SetWidgetState(WidgetState.Loading);
    }

    private void OnMatchmakingError(string errorMessage) 
    {
        widgetSwitcher.ErrorMessage = errorMessage;
        widgetSwitcher.SetWidgetState(WidgetState.Error);
    }

    private void OnSwitcherStateChanged(WidgetState state)
    {
        backButton.enabled = state != WidgetState.Loading;
    }
}
