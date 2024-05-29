// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
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
    
    private List<RectTransform> panels = new List<RectTransform>();
    
    private string cachedSessionId;
    
    private SessionEssentialsWrapper sessionEssentialsWrapper;
    
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
        panels.Except(new []{panel})
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
        panels = new List<RectTransform>()
        {
            defaultPanel,
            creatingPanel,
            joiningPanel,
            joinedPanel,
            failedPanel
        };
        
        sessionEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<SessionEssentialsWrapper>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (sessionEssentialsWrapper == null)
        {
            sessionEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<SessionEssentialsWrapper>();
            SubcribeSessionEvents();
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

    private void OnLeaveSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            MenuManager.Instance.OnBackPressed();
        } 
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            MenuManager.Instance.OnBackPressed();
        }
    }

    private void OnCancelButtonClicked()
    {
        StopAllCoroutines();
        MenuManager.Instance.OnBackPressed();
    }

    private void OnLeaveSessionButtonClicked()
    {
        sessionEssentialsWrapper.LeaveSession(cachedSessionId);
    }
    
    private void OnCreateSessionCompleted(Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            CurrentView = SessionMenuView.Joining;
            StartCoroutine(DelayCallback(sessionView => Helper(result, sessionView)));
        }
        else
        {
            CurrentView = SessionMenuView.Failed;
        }
    }

    private void Helper(Result<SessionV2GameSession> result, SessionMenuView sessionMenuView)
    {
        if (result.IsError)
        {
            CurrentView = SessionMenuView.Failed;
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            return;
        }

        BytewarsLogger.Log(result.Value.id);
        
        CurrentView = sessionMenuView;
        switch (sessionMenuView)
        {
            case SessionMenuView.Joining:
                cachedSessionId = result.Value.id;
                break;
            case SessionMenuView.Joined:
                sessionIdText.text = result.Value.id;
                break;
        }
    }
    
    private void OnEliminationButtonClicked()
    {
        Dictionary<InGameMode, 
        Dictionary<GameSessionServerType, 
        SessionV2GameSessionCreateRequest>> sessionConfig = GameSessionConfig.SessionCreateRequest;
        
        if (!sessionConfig.TryGetValue(InGameMode.None, out var matchTypeDict))
        {
            return;
        }

        if (!matchTypeDict.TryGetValue(GameSessionServerType.None, out var request))
        {
            return;
        }

        CurrentView = SessionMenuView.Creating;
        var button = creatingPanel.gameObject.GetComponentInChildren<Button>();
        button.gameObject.SetActive(true);
        sessionEssentialsWrapper.CreateSession(request);
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
        if (sessionEssentialsWrapper == null)
        {
            return;
        }
        
        SubcribeSessionEvents();
    }

    private void OnDisable()
    {
        if (sessionEssentialsWrapper == null)
        {
            return;
        }

        UnSubcribeSessionEvents();
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

    private void SubcribeSessionEvents()
    {
        sessionEssentialsWrapper.OnCreateSessionCompleteEvent += OnCreateSessionCompleted;
        sessionEssentialsWrapper.OnLeaveSessionCompleteEvent += OnLeaveSessionCompleted;
    }

    private void UnSubcribeSessionEvents()
    {
        sessionEssentialsWrapper.OnCreateSessionCompleteEvent -= OnCreateSessionCompleted;
        sessionEssentialsWrapper.OnLeaveSessionCompleteEvent -= OnLeaveSessionCompleted;
    }

    #endregion
    
}
