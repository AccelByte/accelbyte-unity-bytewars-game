// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

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
    private MatchSessionWrapper matchSessionWrapper;
    private BrowseMatchSessionWrapper_Starter browseMatchSessionWrapper_starter;
    private MatchSessionDSWrapper_Starter matchSessionDSWrapper_starter;
    private MatchSessionP2PWrapper_Starter matchSessionP2PWrapper_starter;

    private bool isEventsListened = false;

    private void Start()
    {
        browseMatchSessionWrapper_starter = TutorialModuleManager.Instance.GetModuleClass<BrowseMatchSessionWrapper_Starter>();
        matchSessionWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionWrapper>();
        matchSessionDSWrapper_starter = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper_Starter>();
        matchSessionP2PWrapper_starter = TutorialModuleManager.Instance.GetModuleClass<MatchSessionP2PWrapper_Starter>();

        BindEvent();
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        refreshBtn.onClick.AddListener(BrowseMatchSession);
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        GameManager.OnDisconnectedInMainMenu += OnDisconnectedFromMainMenu;
        BrowseMatchSession();
    }

    private void OnDestroy()
    {
        backButton.onClick.RemoveAllListeners();
        refreshBtn.onClick.RemoveAllListeners();
        scrollRect.onValueChanged.RemoveAllListeners();
        GameManager.OnDisconnectedInMainMenu -= OnDisconnectedFromMainMenu;
    }

    private void OnEnable()
    {
        if (browseMatchSessionWrapper_starter == null)
        {
            BytewarsLogger.LogWarning("BrowseMatchSessionWrapper is not exist");
            return;
        }

        BrowseMatchSession();
        BindEvent();
    }

    private void OnDisable()
    {
        if (browseMatchSessionWrapper_starter == null)
        {
            BytewarsLogger.LogWarning("BrowseMatchSessionWrapper is not exist");
            return;
        }

        UnbindEvent();
        Reset();
    }

    private void BindEvent()
    {
        if (isEventsListened)
        {
            BytewarsLogger.LogWarning($"Currently listens for wrapper events : {isEventsListened}");
            return;
        }

        browseMatchSessionWrapper_starter.BindEvents();
        matchSessionWrapper.BindEvents();
        matchSessionWrapper.OnJoinedMatchSession += OnJoinedMatchSession;
        matchSessionWrapper.OnCreateOrJoinError += OnCreateOrJoinError;
        //TODO: Copy your matchSessionDSWrapper.BindMatchSessionDSEvents();
        //TODO: Copy your BindMatchSessionP2PEvents here
        isEventsListened = true;
    }

    private void OnCreateOrJoinError(string errorMessage)
    {
        ShowError(errorMessage);
    }

    private void UnbindEvent()
    {
        browseMatchSessionWrapper_starter.UnbindEvents();
        matchSessionWrapper.UnbindEvents();
        //TODO: Copy your matchSessionDSWrapper.UnbindMatchSessionDSEvents();
        //TODO: Copy your UnbindMatchSessionP2PEvents here
        isEventsListened = false;
    }

    #region BrowseMatchSession

    private void BrowseMatchSession()
    {
        ResetList();
        //TODO: Copy your code here
        BytewarsLogger.Log($"Browse Match Session not yet implemented");
        ShowLoading("Getting Match Sessions...", CancelBrowseMatchSession);
    }

    private void OnBrowseMatchSessionFinished(BrowseMatchResult result)
    {
        //TODO: Copy your code here
    }

    private void CancelBrowseMatchSession()
    {
        HideLoadingBackToMainPanel();
        browseMatchSessionWrapper_starter.CancelBrowseMatchSessions();
    }

    #endregion BrowseMatchSession

    #region RetrieveNextPage

    private void OnScrollValueChanged(Vector2 scrollPos)
    {
        //scroll reach bottom
        if (scrollPos.y <= 0)
        {
            //TODO: Copy your code here
        }
    }

    private void OnNextPageMatchSessionsRetrieved(BrowseMatchResult nextPageResult)
    {
        //TODO: Copy your code here
    }

    #endregion RetrieveNextPage
    
    #region JoinMatchSession

    private void JoinMatch(JoinMatchSessionRequest request)
    {
        ShowLoading("Joining Match Session...", CancelJoinMatchSession);
        //TODO: Copy your code here

    }

    private void CancelJoinMatchSession()
    {
        HideLoadingBackToMainPanel();
        //TODO: Copy your code here
    }

    private void OnJoinedMatchSession(string errorMessage)
    {
        if (!string.IsNullOrEmpty(errorMessage))
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

    private void Reset()
    {
        HideError();
        HideLoadingBackToMainPanel();
    }

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
            SessionV2GameSession gameSession = gameSessions[i];
            gameSessionList.Add(gameSession);
            BrowseMatchItemModel model = new BrowseMatchItemModel(gameSession, previousPageCount + i);
            loadedModels.Add(model);
            MatchSessionItem viewItem = GetAvailableViewItem();
            viewItem.SetData(model, JoinMatch);
            instantiatedView.Add(viewItem);
        }
        matchItemContainer.sizeDelta = new Vector2(0, (loadedModels.Count)* ViewItemHeight);
    }

    private void ResetList()
    {
        foreach (MatchSessionItem matchSessionItem in instantiatedView)
        {
            matchSessionItem.gameObject.SetActive(false);
        }
        matchItemContainer.sizeDelta = Vector2.zero;
    }

    private MatchSessionItem GetAvailableViewItem()
    {
        MatchSessionItem instantiatedView = this.instantiatedView.Find(v => !v.gameObject.activeSelf);
        
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