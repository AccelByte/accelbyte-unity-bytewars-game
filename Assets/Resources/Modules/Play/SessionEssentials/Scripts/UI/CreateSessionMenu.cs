// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SessionEssentialsModels;

public class CreateSessionMenu : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform createSessionPanel;
    [SerializeField] private Transform sessionResultPanel;
    [SerializeField] private TMP_Text sessionIdText;
    [SerializeField] private Button createSessionButton;
    [SerializeField] private Button leaveSessionButton;
    [SerializeField] private Button backButton;

    private SessionEssentialsWrapper sessionEssentialsWrapper;

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        createSessionButton.onClick.AddListener(CreateSession);
        widgetSwitcher.OnRetryButtonClicked = CreateSession;
        leaveSessionButton.onClick.AddListener(LeaveSession);
    }

    private void OnEnable()
    {
        // Reset to default state.
        createSessionPanel.gameObject.SetActive(true);
        sessionResultPanel.gameObject.SetActive(false);
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);

        sessionEssentialsWrapper ??= TutorialModuleManager.Instance.GetModuleClass<SessionEssentialsWrapper>();
        if (!sessionEssentialsWrapper)
        {
            return;
        }

        // Show the last session result if any.
        if (AccelByteWarsOnlineSession.CachedSession != null)
        {
            OnCreateSessionComplete(Result<SessionV2GameSession>.CreateOk(AccelByteWarsOnlineSession.CachedSession));
        }
    }

    private void CreateSession()
    {
        SessionV2GameSessionCreateRequest request = 
            AccelByteWarsOnlineSessionModels.GetGameSessionRequestModel(InGameMode.None, GameSessionServerType.None);
        if (request == null)
        {
            widgetSwitcher.ErrorMessage = InvalidSessionTypeMessage;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        widgetSwitcher.LoadingMessage = CreatingSessionMessage;
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        sessionEssentialsWrapper.CreateGameSession(request, OnCreateSessionComplete);
    }

    private void OnCreateSessionComplete(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            widgetSwitcher.ErrorMessage = result.Error.Message;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        sessionIdText.text = result.Value.id;
        createSessionPanel.gameObject.SetActive(false);
        sessionResultPanel.gameObject.SetActive(true);
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
    }

    private void LeaveSession()
    {
        widgetSwitcher.LoadingMessage = LeavingSessionMessage;
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        sessionEssentialsWrapper.LeaveGameSession(sessionIdText.text, OnLeaveSessionComplete);
    }

    private void OnLeaveSessionComplete(Result result)
    {
        if (result.IsError)
        {
            widgetSwitcher.ErrorMessage = result.Error.Message;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        createSessionPanel.gameObject.SetActive(true);
        sessionResultPanel.gameObject.SetActive(false);
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
    }

    public override GameObject GetFirstButton()
    {
        return AccelByteWarsOnlineSession.CachedSession == null ? createSessionButton.gameObject : leaveSessionButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateSessionMenu;
    }
}
