// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using static SessionEssentialsModels;
using static MatchSessionEssentialsModels;

public class MatchSessionStateSwitcher : MonoBehaviour
{
    [SerializeField] private MatchSessionMenuState DefaultState = MatchSessionMenuState.CreateMatch;

    public Action OnRetryButtonClicked = delegate { };
    public Action OnCancelButtonClicked = delegate { };

    public string LoadingMessage
    {
        get => loadingMessageText.text.ToString();
        set
        {
            loadingMessageText.text = value;
        }
    }

    public string SubLoadingMessage
    {
        get => subLoadingMessageText.text.ToString();
        set
        {
            subLoadingMessageText.text = value;
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

    public MatchSessionMenuState CurrentState { get; private set; } = MatchSessionMenuState.CreateMatch;
    public GameObject DesiredFocus { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private GameObject countdownPanel;

    [SerializeField] private TMP_Text loadingMessageText;
    [SerializeField] private TMP_Text subLoadingMessageText;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text countdownText;

    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;

    private CancellationTokenSource ctsRequestingServer;

    private void Awake()
    {
        retryButton.onClick.AddListener(() => OnRetryButtonClicked?.Invoke());
        cancelButton.onClick.AddListener(() => OnCancelButtonClicked?.Invoke());
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        DesiredFocus = cancelButton.gameObject;
    }

    private void OnDisable()
    {
        ctsRequestingServer?.Cancel();
    }

    public void OnValidate()
    {
        SetState(DefaultState);
    }

    public void SetState(MatchSessionMenuState newState)
    {
        CurrentState = newState;

        // Toggle panels
        loadingPanel.gameObject.SetActive(CurrentState != MatchSessionMenuState.Error);
        errorPanel.gameObject.SetActive(CurrentState == MatchSessionMenuState.Error);

        // Cancel countdown timer.
        if (CurrentState != MatchSessionMenuState.RequestingServer)
        {
            ctsRequestingServer?.Cancel();
        }

        // Hide all buttons and sub-message by default
        retryButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        subLoadingMessageText.gameObject.SetActive(false);
        countdownPanel.gameObject.SetActive(false);

        // State-specific logic
        switch (CurrentState)
        {
            case MatchSessionMenuState.CreateMatch:
                LoadingMessage = CreatingMatchMessage;
                cancelButton.gameObject.SetActive(true);
                DesiredFocus = cancelButton.gameObject;
                break;
            case MatchSessionMenuState.JoinMatch:
                LoadingMessage = JoiningMatchMessage;
                cancelButton.gameObject.SetActive(true);
                DesiredFocus = cancelButton.gameObject;
                break;
            case MatchSessionMenuState.JoinedMatch:
                LoadingMessage = JoiningMatchMessage;
                subLoadingMessageText.gameObject.SetActive(true);
                SubLoadingMessage = MatchJoinedMessage;
                break;
            case MatchSessionMenuState.LeaveMatch:
                LoadingMessage = LeavingMatchMessage;
                break;
            case MatchSessionMenuState.RequestingServer:
                LoadingMessage = RequestingServerMessage;
                cancelButton.gameObject.SetActive(true);
                countdownPanel.gameObject.SetActive(true);
                DesiredFocus = cancelButton.gameObject;
                ctsRequestingServer = new();
                AccelByteWarsUtility.StartCountdown(
                    RequestingServerTimeout,
                    (float remainingTime, bool isComplete) =>
                    {
                        if (!isActiveAndEnabled)
                        {
                            ctsRequestingServer?.Cancel();
                            return;
                        }
                        countdownText.text = string.Format(RequestingServerTimerMessage, (int)remainingTime);
                        if (isComplete) cancelButton.onClick?.Invoke();
                    },
                    ctsRequestingServer.Token).Forget();
                break;
            case MatchSessionMenuState.Error:
                retryButton.gameObject.SetActive(true);
                backButton.gameObject.SetActive(true);
                DesiredFocus = retryButton.gameObject;
                break;
        }
    }
}
