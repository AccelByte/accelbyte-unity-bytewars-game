// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class QuickPlayMenuHandler : MenuCanvas
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button eliminationButton;
    [SerializeField] private Button teamDeathmatchButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button okButton;
    
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private GameObject findingMatchPanel;
    [SerializeField] private GameObject joiningMatchPanel;
    [SerializeField] private GameObject cancelingMatchPanel;
    [SerializeField] private GameObject failedPanel;
    [SerializeField] private GameObject footerButtonPanel;
    [SerializeField] private GameObject headerPanel;

    private List<GameObject> _panels = new List<GameObject>();

    public static event Action OnMenuEnable;
    public static event Action OnMenuDisable;

    private MatchmakingSessionDSWrapper _matchmakingSessionDSWrapper;
    
    #region QuickPlayView

    public enum QuickPlayView
    {
        Default,
        FindingMatch,
        JoiningMatch,
        CancelingMatch,
        Failed
    }

    private QuickPlayView currentView
    {
        get => currentView;
        set => viewSwitcher(value);
    }

    private void viewSwitcher(QuickPlayView value)
    {
        
        switch (value)
        {
            case QuickPlayView.FindingMatch:
                switcherHelper(findingMatchPanel, value);
                break;
            case QuickPlayView.JoiningMatch:
                switcherHelper(joiningMatchPanel, value);
                break;
            case QuickPlayView.CancelingMatch:
                switcherHelper(cancelingMatchPanel, value);
                break;
            case QuickPlayView.Failed:
                switcherHelper(failedPanel, value);
                break;
            case QuickPlayView.Default:
                switcherHelper(contentPanel, value);
                break;
        }
    }

    private void switcherHelper(GameObject panel, QuickPlayView value)
    {
        panel.SetActive(true);
        _panels.Except(new []{panel})
            .ToList().ForEach(x => x.SetActive(false));
        if (value != QuickPlayView.Default)
        {
            headerPanel.SetActive(false);
            footerButtonPanel.SetActive(false);
            return;
        }
        
        headerPanel.SetActive(true);
        footerButtonPanel.SetActive(true);
    }

    #endregion

    private void Awake()
    {
        _panels = new List<GameObject>()
        {
            contentPanel, 
            findingMatchPanel, 
            joiningMatchPanel, 
            cancelingMatchPanel, 
            failedPanel
        };
    }
    

    private void Start()
    {
        if (_matchmakingSessionDSWrapper == null)
        {
            _matchmakingSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();
        }

        BindMatchmakingEvent();
        
        eliminationButton.onClick.AddListener(OnEliminationButtonClicked);
        teamDeathmatchButton.onClick.AddListener(OnTeamDeathMatchButtonClicked);
        cancelButton.onClick.AddListener(OnCancelMatchmakingClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
        okButton.onClick.AddListener(OnOKFailedButtonClicked);
    }
    
    private void OnEnable()
    {
        OnMenuEnable?.Invoke();

        currentView = QuickPlayView.Default;

        if (_matchmakingSessionDSWrapper == null)
        {
            _matchmakingSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();

            if (_matchmakingSessionDSWrapper != null)
            {
                BindMatchmakingEvent();
            }
        }
    }

    private void BindMatchmakingEvent()
    {
        // listen event when match is found and ds available
        _matchmakingSessionDSWrapper.OnMatchmakingFoundEvent += JoinSessionPanel;
        _matchmakingSessionDSWrapper.OnDSAvailableEvent += TravelToGame;
        
        // listen event when failed
        _matchmakingSessionDSWrapper.OnStartMatchmakingFailed += FailedPanel;
        _matchmakingSessionDSWrapper.OnMatchmakingJoinSessionFailedEvent += FailedPanel;
        _matchmakingSessionDSWrapper.OnDSFailedRequestEvent += FailedPanel;
        _matchmakingSessionDSWrapper.OnSessionEnded += FailedPanel;
        _matchmakingSessionDSWrapper.OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    private void UnbindMatchmakingEvents()
    {
        //remove all events
        _matchmakingSessionDSWrapper.OnMatchmakingFoundEvent -= JoinSessionPanel;
        _matchmakingSessionDSWrapper.OnDSAvailableEvent -= TravelToGame;
        _matchmakingSessionDSWrapper.OnStartMatchmakingFailed -= FailedPanel;
        _matchmakingSessionDSWrapper.OnMatchmakingJoinSessionFailedEvent += FailedPanel;
        _matchmakingSessionDSWrapper.OnDSFailedRequestEvent -= FailedPanel;
        _matchmakingSessionDSWrapper.OnSessionEnded -= FailedPanel;
        _matchmakingSessionDSWrapper.OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    private void OnCancelMatchmakingComplete()
    {
        currentView = QuickPlayView.Default;
    }

    private void OnDisable()
    {        
        OnMenuDisable?.Invoke();
        if (_matchmakingSessionDSWrapper == null) return;
        UnbindMatchmakingEvents();
    }
    
    private void OnEliminationButtonClicked()
    {
        currentView = QuickPlayView.FindingMatch;
        _matchmakingSessionDSWrapper.StartDSMatchmaking("unity-elimination-ds");
    }
    
    private void OnTeamDeathMatchButtonClicked()
    {
        currentView = QuickPlayView.FindingMatch;
        _matchmakingSessionDSWrapper.StartDSMatchmaking("unity-teamdeathmatch-ds");
    }

    private void OnCancelMatchmakingClicked()
    {
        currentView = QuickPlayView.CancelingMatch;
        _matchmakingSessionDSWrapper.CancelDSMatchmaking();
    }
    
    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    private void OnOKFailedButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    private void FailedPanel()
    {
        currentView = QuickPlayView.Failed;
    }
    
    private void JoinSessionPanel(string sessionId)
    {
        currentView = QuickPlayView.JoiningMatch;
    }
    
    private void TravelToGame(SessionV2GameSession session)
    {
        _matchmakingSessionDSWrapper.TravelToDS(session);
    }
    
    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.QuickPlayMenuCanvas;
    }
}