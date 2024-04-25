// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionMenuHandler : MenuCanvas
{
    [SerializeField] private Button createEliminationButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button backFailedButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button cancelButton;
    
    [SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform creatingPanel;
    [SerializeField] private RectTransform joiningPanel;
    [SerializeField] private RectTransform joinedPanel;
    [SerializeField] private RectTransform failedPanel;
    
    [SerializeField] private RectTransform footerPanel;
    [SerializeField] private TMP_Text sessionIdText;
    
    private List<RectTransform> _panels = new List<RectTransform>();

    private SessionRequestPayload _sessionRequestPayload;
    
    private SessionResponsePayload _sessionResponsePayload;
    
    private SessionEssentialsWrapper _sessionEssentialsWrapper;
    
    private static readonly TutorialType _tutorialType = TutorialType.SessionEssentials;

    public enum SessionMenuView
    {
        Default,
        Creating,
        Failed,
        Joining,
        Joined
    }

    private SessionMenuView CurrentView
    {
        get => CurrentView;
        set => viewSwitcher(value);
    }

    private void viewSwitcher(SessionMenuView value)
    {
        switch (value)
        {
            case SessionMenuView.Default:
                switcherHelper(defaultPanel, value);
                break;
            case SessionMenuView.Creating:
                switcherHelper(creatingPanel, value);
                break;
            case SessionMenuView.Failed:
                switcherHelper(failedPanel, value);
                break;
            case SessionMenuView.Joining:
                switcherHelper(joiningPanel, value);
                break;
            case SessionMenuView.Joined:
                switcherHelper(joinedPanel, value);
                break;
        }
    }

    private void switcherHelper(RectTransform panel, SessionMenuView value)
    {
        panel.gameObject.SetActive(true);
        _panels.Except(new []{panel})
            .ToList().ForEach(x => x.gameObject.SetActive(false));
        if (value != SessionMenuView.Default)
        {
            footerPanel.gameObject.SetActive(false);
            return;
        }
        
        footerPanel.gameObject.SetActive(true);
    }

    private void Awake()
    {
        _panels = new List<RectTransform>()
        {
            defaultPanel,
            creatingPanel,
            joiningPanel,
            joinedPanel,
            failedPanel
        };
        
        _sessionEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<SessionEssentialsWrapper>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (_sessionEssentialsWrapper == null)
        {
            _sessionEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<SessionEssentialsWrapper>();
            BindToWrapperEvent();
        }
        
        createEliminationButton.onClick.AddListener(OnEliminationButtonClicked);
        leaveButton.onClick.AddListener(OnLeaveSessionButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        backFailedButton.onClick.AddListener(OnBackFailedButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }
    
    private void OnBackFailedButtonClicked()
    {
        CurrentView = SessionMenuView.Default;
    }
    
    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    private void OnLeaveSessionCompleted(SessionResponsePayload response)
    {
        if (response.IsError) return;
        IsTutorialTypeMatch(_sessionResponsePayload.TutorialType);
        _sessionResponsePayload = null;
        MenuManager.Instance.OnBackPressed();
    }

    private void OnCancelButtonClicked()
    {
        StopAllCoroutines();
        MenuManager.Instance.OnBackPressed();
    }

    private void OnLeaveSessionButtonClicked()
    {
        _sessionEssentialsWrapper.LeaveSession(_sessionResponsePayload.Result.Value.id);
    }

    private void IsTutorialTypeMatch(TutorialType? tutorialType)
    {
        if (_tutorialType != tutorialType)
        {
            BytewarsLogger.LogWarning($"{tutorialType} is not match with {_tutorialType}");
            return;
        }
        BytewarsLogger.Log($"{tutorialType} is match with {_tutorialType}");
    }
    
    private void OnCreateSessionCompleted(SessionResponsePayload response)
    {
        if (!response.IsError)
        {
            CurrentView = SessionMenuView.Joining;
            StartCoroutine(DelayCallback(sessionView => Helper(response, sessionView)));
        }
    }

    private void Helper(SessionResponsePayload response, SessionMenuView sessionMenuView)
    {
        if (response.IsError)
        {
            CurrentView = SessionMenuView.Failed;
            BytewarsLogger.LogWarning($"{response.Result.Error.Message}");
            return;
        }

        BytewarsLogger.Log(response.Result.Value.id);
        
        CurrentView = sessionMenuView;
        switch (sessionMenuView)
        {
            case SessionMenuView.Joining:
                _sessionResponsePayload = response;
                break;
            case SessionMenuView.Joined:
                sessionIdText.text = _sessionResponsePayload.Result.Value.id;
                break;
        }
    }
    
    private void OnEliminationButtonClicked()
    {
        var sessionRequest = new SessionRequestPayload
        {
            SessionType = SessionType.none,
            InGameMode = InGameMode.None,
            SessionTemplateName = "unity-elimination-none",
            TutorialType = TutorialType.SessionEssentials
        };

        CurrentView = SessionMenuView.Creating;
        var button = creatingPanel.gameObject.GetComponentInChildren<Button>();
        button.gameObject.SetActive(true);
        _sessionEssentialsWrapper.CreateSession(sessionRequest);
    }
    
    private IEnumerator DelayCallback(Action<SessionMenuView> action)
    {
        yield return new WaitForSeconds(1);
        action?.Invoke(SessionMenuView.Joining);
        yield return new WaitForSeconds(1);
        action?.Invoke(SessionMenuView.Joined);
    }

    private void OnEnable()
    {
        CurrentView = SessionMenuView.Default;
        if (_sessionEssentialsWrapper == null)
        {
            return;
        }
        
        BindToWrapperEvent();
    }

    private void OnDisable()
    {
        if (_sessionEssentialsWrapper == null)
        {
            return;
        }

        UnbindToWrapperEvent();
    }

    public override GameObject GetFirstButton()
    {
        return createEliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SessionEssentialsMenuCanvas;
    }

    #region EventListener

    private void BindToWrapperEvent()
    {
        _sessionEssentialsWrapper.OnCreateSessionCompleteEvent += OnCreateSessionCompleted;
        _sessionEssentialsWrapper.OnLeaveSessionCompleteEvent += OnLeaveSessionCompleted;
    }

    private void UnbindToWrapperEvent()
    {
        _sessionEssentialsWrapper.OnCreateSessionCompleteEvent -= OnCreateSessionCompleted;
        _sessionEssentialsWrapper.OnLeaveSessionCompleteEvent -= OnLeaveSessionCompleted;
    }

    #endregion
    
}
