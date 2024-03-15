using System;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BrowseMatchMenuCanvas_Starter : MenuCanvas
{
    [SerializeField] private MatchSessionItem matchSessionItemPrefab;
    [SerializeField] private RectTransform matchItemContainer;
    [SerializeField] private Button refreshBtn;
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private GameObject noMatchFoundInfo;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private ErrorPanel errorPanel;
    private readonly List<BrowseMatchItemModel> loadedModels = new List<BrowseMatchItemModel>();
    private readonly List<MatchSessionItem> instantiatedView = new List<MatchSessionItem>();
    private readonly List<SessionV2GameSession> gameSessionList = new List<SessionV2GameSession>();
    private const float ViewItemHeight = 75;

    private BrowseMatchSessionWrapper_Starter browseMatchSessionWrapper_Starter;
    private MatchSessionDSWrapper_Starter matchSessionDSWrapper;
    
    private void Start()
    {
        browseMatchSessionWrapper_Starter = TutorialModuleManager.Instance.GetModuleClass<BrowseMatchSessionWrapper_Starter>();
        matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper_Starter>();

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        refreshBtn.onClick.AddListener(BrowseMatchSession);
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        BrowseMatchSessionEventListener.Init(gameSessionList);
        BrowseMatchSessionEventListener.OnUpdate = OnGameSessionUpdated;
        GameManager.OnDisconnectedInMainMenu += OnDisconnectedFromMainMenu;
        BrowseMatchSession();
    }
    
    #region BrowseMatchSession
    private void BrowseMatchSession()
    {
        // Copy BrowseMatchSession Code here
        BytewarsLogger.Log("Browse Match Session not yet implemented");
    }

    private void OnBrowseMatchSessionFinished(BrowseMatchResult result)
    {
        // Copy OnBrowseMatchSessionFinished here
    }

    private void CancelBrowseMatchSession()
    {
        HideLoadingBackToMainPanel();
        browseMatchSessionWrapper_Starter.CancelBrowseMatchSessions();
    }
    #endregion BrowseMatchSession

    #region RetrieveNextPage
    private void OnScrollValueChanged(Vector2 scrollPos)
    {
        // Copy OnScrollValueChanged here
    }

    private void OnNextPageMatchSessionsRetrieved(BrowseMatchResult nextPageResult)
    {
        // Copy OnNextPageMatchSessionsRetrieved here
    }
    #endregion RetrieveNextPage
    
    #region JoinMatchSession
    private void JoinMatch(JoinMatchSessionRequest request)
    {
        ShowLoading("Joining Match Session...", CancelJoinMatchSession);
        matchSessionDSWrapper
            .JoinMatchSession(request.MatchSessionId, request.GameMode, OnJoinedMatchSession);
    }

    private void CancelJoinMatchSession()
    {
        HideLoadingBackToMainPanel();
        matchSessionDSWrapper.CancelJoinMatchSession();
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
        var updatedModel = loadedModels.Find(m => m.MatchSessionId == result.id);
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
            gameSessionList.Add(gameSession);
            var model = new BrowseMatchItemModel(gameSession, previousPageCount + i);
            loadedModels.Add(model);
            var viewItem = GetAvailableViewItem();
            viewItem.SetData(model, JoinMatch);
            instantiatedView.Add(viewItem);
        }
        matchItemContainer.sizeDelta = new Vector2(0, (loadedModels.Count)* ViewItemHeight);
    }

    private void Reset()
    {
        foreach (var matchSessionItem in instantiatedView)
        {
            matchSessionItem.gameObject.SetActive(false);
        }
        matchItemContainer.sizeDelta = Vector2.zero;
    }

    private MatchSessionItem GetAvailableViewItem()
    {
        var instantiatedView = 
            this.instantiatedView.Find(v => !v.gameObject.activeSelf);
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