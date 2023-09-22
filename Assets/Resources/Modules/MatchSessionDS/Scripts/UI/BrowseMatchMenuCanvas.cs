using System;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BrowseMatchMenuCanvas : MenuCanvas
{
    [SerializeField] private MatchSessionItem matchSessionItemPrefab;
    [SerializeField] private RectTransform matchItemContainer;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private GameObject noMatchFoundInfo;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private ErrorPanel errorPanel;
    private readonly List<BrowseMatchItemModel> _loadedModels = new List<BrowseMatchItemModel>();
    private readonly List<MatchSessionItem> _instantiatedView = new List<MatchSessionItem>();
    private readonly List<SessionV2GameSession> _gameSessions = new List<SessionV2GameSession>();
    private const float ViewItemHeight = 75;

    private BrowseMatchSessionWrapper _browseMatchSessionWrapper;
    private MatchSessionDSWrapper _matchSessionDSWrapper;
    
    private void Start()
    {
        _browseMatchSessionWrapper = TutorialModuleManager.Instance.GetModuleClass<BrowseMatchSessionWrapper>();
        _matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();

        // OLD
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        refreshBtn.onClick.AddListener(BrowseMatchSession);
        _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        BrowseMatchSessionEventListener.Init(_gameSessions);
        BrowseMatchSessionEventListener.OnUpdate = OnGameSessionUpdated;
        GameManager.OnDisconnectedInMainMenu += OnDisconnectedFromMainMenu;
        BrowseMatchSession();
    }
    
    #region BrowseMatchSession
    private void BrowseMatchSession()
    {
        Reset();
        _browseMatchSessionWrapper.BrowseMatch(OnBrowseMatchSessionFinished);
        ShowLoading("Getting Match Sessions...", CancelBrowseMatchSession);
    }
    private void OnBrowseMatchSessionFinished(BrowseMatchResult result)
    {
        if (String.IsNullOrEmpty(result.ErrorMessage))
        {
            HideLoadingBackToMainPanel();
            if (result.Result.Length<1)
            {
                noMatchFoundInfo.SetActive(true);
            }
            else
            {
                noMatchFoundInfo.SetActive(false);
                RenderResult(result.Result);
            }
        }
        else
        {
            ShowError(result.ErrorMessage);
        }
    }
    private void CancelBrowseMatchSession()
    {
        HideLoadingBackToMainPanel();
        _browseMatchSessionWrapper.CancelBrowseMatchSessions();
    }
    #endregion BrowseMatchSession

    #region RetrieveNextPage
    private void OnScrollValueChanged(Vector2 scrollPos)
    {
        //scroll reach bottom
        if (scrollPos.y <= 0)
        {
            _browseMatchSessionWrapper.QueryNextMatchSessions(OnNextPageMatchSessionsRetrieved);
        }
    }
    private void OnNextPageMatchSessionsRetrieved(BrowseMatchResult nextPageResult)
    {
        if (String.IsNullOrEmpty(nextPageResult.ErrorMessage))
        {
            RenderResult(nextPageResult.Result, _loadedModels.Count);
        }
        else
        {
            ShowError(nextPageResult.ErrorMessage);
        }
    }
    #endregion RetrieveNextPage
    
    #region JoinMatchSession
    private void JoinMatch(JoinMatchSessionRequest request)
    {
        ShowLoading("Joining Match Session...", CancelJoinMatchSession);
        _matchSessionDSWrapper
            .JoinMatchSession(request.MatchSessionId, request.GameMode, OnJoinedMatchSession);
    }
    private void CancelJoinMatchSession()
    {
        HideLoadingBackToMainPanel();
        _matchSessionDSWrapper.CancelJoinMatchSession();
    }
    private void OnJoinedMatchSession(string errorMessage)
    {
        //success joined match session will be handled by BrowseMatchSessionWrapper
        if (!String.IsNullOrEmpty(errorMessage))
        {
            ShowError($"Join Match Session Failed: {errorMessage}");
        }
    }
    #endregion JoinMatchSession

    #region EventCallback
    private void OnDisconnectedFromMainMenu(string disconnectReason)
    {
        ShowError($"disconnected from server, reason:{disconnectReason}");
    }

    private void OnGameSessionUpdated(SessionV2GameSession result)
    {
        var updatedModel = _loadedModels.Find(m => m.MatchSessionId == result.id);
        updatedModel?.Update(result);
        var currentMenu = MenuManager.Instance.GetCurrentMenu();
        if (currentMenu is MatchLobbyMenu matchLobbyMenu)
        {
            matchLobbyMenu.Refresh();
        }
    }
    #endregion EventCallback

    #region ViewState
    private void HideLoadingBackToMainPanel()
    {
        loadingPanel.gameObject.SetActive(false);
        mainPanel.gameObject.SetActive(true);
    }
    
    private void ShowError(string errorMessage)
    {
        mainPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(false);
        errorPanel.Show(errorMessage, HideError);
    }

    private void ShowLoading(string loadingInfo, UnityAction cancelCallback=null)
    {
        mainPanel.gameObject.SetActive(false);
        loadingPanel.Show(loadingInfo, cancelCallback);
        errorPanel.gameObject.SetActive(false);
    }

    private void HideError()
    {
        errorPanel.gameObject.SetActive(false);
        mainPanel.gameObject.SetActive(true);
    }
    #endregion ViewState
    
    private void RenderResult(SessionV2GameSession[] gameSessions, int previousPageCount=0)
    {
        for (var i = 0; i < gameSessions.Length; i++)
        {
            var gameSession = gameSessions[i];
            _gameSessions.Add(gameSession);
            var model = new BrowseMatchItemModel(gameSession, previousPageCount + i);
            _loadedModels.Add(model);
            var viewItem = GetAvailableViewItem();
            viewItem.SetData(model, JoinMatch);
            _instantiatedView.Add(viewItem);
        }
        matchItemContainer.sizeDelta = new Vector2(0, (_loadedModels.Count)* ViewItemHeight);
    }
    private void Reset()
    {
        foreach (var matchSessionItem in _instantiatedView)
        {
            matchSessionItem.gameObject.SetActive(false);
        }
        matchItemContainer.sizeDelta = Vector2.zero;
    }
    private MatchSessionItem GetAvailableViewItem()
    {
        var instantiatedView = 
            _instantiatedView.Find(v => !v.gameObject.activeSelf);
        if (instantiatedView == null)
        {
            return Instantiate(matchSessionItemPrefab, matchItemContainer, false);
        }
        else
        {
            return instantiatedView;
        }
    }
    
    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return refreshBtn.gameObject;
    }
    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BrowseMatchMenuCanvas;
    }
    #endregion MenuCanvasOverride
}