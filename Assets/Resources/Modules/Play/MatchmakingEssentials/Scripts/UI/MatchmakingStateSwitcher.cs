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
using static MatchmakingEssentialsModels;

public class MatchmakingStateSwitcher : MonoBehaviour
{
    [SerializeField] private MatchmakingMenuState DefaultState = MatchmakingMenuState.FindingMatch;

    public Action OnJoinButtonClicked = delegate { };
    public Action OnRejectButtonClicked = delegate { };
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

    public MatchmakingMenuState CurrentState { get; private set; } = MatchmakingMenuState.FindingMatch;
    public GameObject DesiredFocus { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private GameObject countdownPanel;

    [SerializeField] private TMP_Text loadingMessageText;
    [SerializeField] private TMP_Text subLoadingMessageText;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text countdownText;

    [SerializeField] private Button joinButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backButton;

    private CancellationTokenSource ctsJoinConfirmation;
    private CancellationTokenSource ctsRequestingServer;

    private void Awake()
    {
        joinButton.onClick.AddListener(() => OnJoinButtonClicked?.Invoke());
        rejectButton.onClick.AddListener(() => OnRejectButtonClicked?.Invoke());
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
        ctsJoinConfirmation?.Cancel();
        ctsRequestingServer?.Cancel();
    }

    public void OnValidate()
    {
        SetState(DefaultState);
    }

    public void SetState(MatchmakingMenuState newState)
    {
        CurrentState = newState;

        // Toggle panels
        loadingPanel.gameObject.SetActive(CurrentState != MatchmakingMenuState.Error);
        errorPanel.gameObject.SetActive(CurrentState == MatchmakingMenuState.Error);

        // Cancel countdown timer.
        if (CurrentState != MatchmakingMenuState.JoinMatchConfirmation)
        {
            ctsJoinConfirmation?.Cancel();
        }
        if (CurrentState != MatchmakingMenuState.RequestingServer)
        {
            ctsRequestingServer?.Cancel();
        }

        // Hide all buttons and sub-message by default
        joinButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        subLoadingMessageText.gameObject.SetActive(false);
        countdownPanel.gameObject.SetActive(false);

        // State-specific logic
        switch (CurrentState)
        {
            case MatchmakingMenuState.StartMatchmaking:
                LoadingMessage = StartMatchmakingMessage;
                break;
            case MatchmakingMenuState.FindingMatch:
                LoadingMessage = FindingMatchMessage;
                cancelButton.gameObject.SetActive(true);
                DesiredFocus = cancelButton.gameObject;
                break;
            case MatchmakingMenuState.MatchFound:
                LoadingMessage = FindingMatchMessage;
                subLoadingMessageText.gameObject.SetActive(true);
                SubLoadingMessage = MatchFoundMessage;
                break;
            case MatchmakingMenuState.CancelMatchmaking:
                LoadingMessage = CancelMatchmakingMessage;
                break;
            case MatchmakingMenuState.JoinMatchConfirmation:
                LoadingMessage = JoinMatchConfirmationMessage;
                joinButton.gameObject.SetActive(true);
                rejectButton.gameObject.SetActive(true);
                countdownPanel.gameObject.SetActive(true);
                DesiredFocus = joinButton.gameObject;
                ctsJoinConfirmation = new();
                AccelByteWarsUtility.StartCountdown(
                    JoinMatchConfirmationTimeout,
                    (float remainingTime, bool isComplete) =>
                    {
                        if (!isActiveAndEnabled)
                        {
                            ctsJoinConfirmation?.Cancel();
                            return;
                        }
                        countdownText.text = string.Format(AutoJoinSessionMessage, (int)remainingTime);
                        if (isComplete) joinButton.onClick?.Invoke();
                    },
                    ctsJoinConfirmation.Token).Forget();
                break;
            case MatchmakingMenuState.JoinMatch:
                LoadingMessage = JoiningMatchMessage;
                cancelButton.gameObject.SetActive(true);
                DesiredFocus = cancelButton.gameObject;
                break;
            case MatchmakingMenuState.JoinedMatch:
                LoadingMessage = JoiningMatchMessage;
                subLoadingMessageText.gameObject.SetActive(true);
                SubLoadingMessage = MatchJoinedMessage;
                break;
            case MatchmakingMenuState.LeaveMatch:
                LoadingMessage = LeavingMatchMessage;
                break;
            case MatchmakingMenuState.RejectMatch:
                LoadingMessage = RejectingMatchMessage;
                break;
            case MatchmakingMenuState.RequestingServer:
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
            case MatchmakingMenuState.Error:
                retryButton.gameObject.SetActive(true);
                backButton.gameObject.SetActive(true);
                DesiredFocus = retryButton.gameObject;
                break;
        }
    }
}
