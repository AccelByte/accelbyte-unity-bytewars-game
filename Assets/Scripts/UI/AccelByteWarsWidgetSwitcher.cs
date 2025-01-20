// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccelByteWarsWidgetSwitcher : MonoBehaviour
{
    [Serializable]
    public enum WidgetState 
    {
        Not_Empty,
        Empty,
        Loading,
        Error
    }

    public Action OnCancelButtonClicked = delegate { };
    public Action OnRetryButtonClicked = delegate { };
    public Action<WidgetState> OnStateChanged = delegate { };

    public string EmptyMessage 
    {
        get => emptyMessageText.text.ToString();
        set
        {
            emptyMessageText.text = value;
        }
    }
    public string LoadingMessage
    {
        get => loadingMessageText.text.ToString();
        set
        {
            loadingMessageText.text = value;
        }
    }
    public string ErrorMessage
    {
        get => errorMessageText.text.ToString();
        set
        {
            errorMessageText.text = value;
        }
    }

    [Header("Attributes")]
    [SerializeField] public WidgetState DefaultState = WidgetState.Not_Empty;
    public WidgetState CurrentState { get; private set; } = WidgetState.Not_Empty;

    [SerializeField] private string DefaultEmptyMessage = "No entry";
    [SerializeField] private string DefaultLoadingMessage = "Loading";
    [SerializeField] private string DefaultErrorMessage = "An error occurred";

    [Header("Components")]
    [SerializeField] private GameObject notEmptyStatePanel;
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private GameObject loadingStatePanel;
    [SerializeField] private GameObject errorStatePanel;

    [SerializeField] private TMP_Text emptyMessageText;
    [SerializeField] private TMP_Text loadingMessageText;
    [SerializeField] private TMP_Text errorMessageText;

    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;

    [Header("Cancel Action")]
    [SerializeField] private bool enableCancelButton = false;
    [SerializeField] private bool showCancelButtonOnLoading = false;
    [SerializeField] private bool showCancelButtonOnEmpty = false;
    [SerializeField] private bool showCancelButtonOnNotEmpty = false;
    [SerializeField] private bool showCancelButtonOnError = false;

    [Header("Retry Action")]
    [SerializeField] private bool enableRetryButton = false;
    [SerializeField] private bool showRetryButtonOnLoading = false;
    [SerializeField] private bool showRetryButtonOnEmpty = false;
    [SerializeField] private bool showRetryButtonOnNotEmpty = false;
    [SerializeField] private bool showRetryButtonOnError = false;

    public void SetWidgetState(WidgetState newState) 
    {
        if (CurrentState == newState)
        {
            return;
        }

        bool showCancelButton = false, showRetryButton = false;
        switch(newState) 
        {
            case WidgetState.Not_Empty:
                showRetryButton = showRetryButtonOnNotEmpty;
                showCancelButton = showCancelButtonOnNotEmpty;
                break;
            case WidgetState.Empty:
                showRetryButton = showRetryButtonOnEmpty;
                showCancelButton = showCancelButtonOnEmpty;
                break;
            case WidgetState.Loading:
                showRetryButton = showRetryButtonOnLoading;
                showCancelButton = showCancelButtonOnLoading;
                break;
            case WidgetState.Error:
                showRetryButton = showRetryButtonOnError;
                showCancelButton = showCancelButtonOnError;
                break;
        }

        notEmptyStatePanel.gameObject.SetActive(newState == WidgetState.Not_Empty);
        emptyStatePanel.gameObject.SetActive(newState == WidgetState.Empty);
        loadingStatePanel.gameObject.SetActive(newState == WidgetState.Loading);
        errorStatePanel.gameObject.SetActive(newState == WidgetState.Error);

        retryButton.gameObject.SetActive(enableRetryButton && showRetryButton);
        cancelButton.gameObject.SetActive(enableCancelButton && showCancelButton);

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }

    private void OnValidate()
    {
        EmptyMessage = DefaultEmptyMessage;
        LoadingMessage = DefaultLoadingMessage;
        ErrorMessage = DefaultErrorMessage;

        SetWidgetState(DefaultState);
    }

    private void OnEnable()
    {
        retryButton.onClick.AddListener(OnRetryButtonClicked.Invoke);
        cancelButton.onClick.AddListener(OnCancelButtonClicked.Invoke);
    }

    private void OnDisable()
    {
        retryButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
    }
}
