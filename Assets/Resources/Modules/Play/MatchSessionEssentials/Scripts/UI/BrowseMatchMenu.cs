// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using static MatchSessionEssentialsModels;

public class BrowseMatchMenu : MenuCanvas
{
    public delegate void BrowseMatchHandler(string pageUrl, ResultCallback<BrowseSessionResult> onComplete);

    public static event BrowseMatchHandler OnBrowseDSMatch = delegate { };
    public static event BrowseMatchHandler OnBrowseP2PMatch = delegate { };

    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private BrowseMatchEntry sessionEntryPrefab;
    [SerializeField] private Transform sessionListPanel;
    [SerializeField] private ScrollRect sessionListScrollRect;
    [SerializeField] private GameObject listLoaderPanel;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;

    private ModuleModel matchSessionDSModule, matchSessionP2PModule;
    
    private Dictionary<string, BrowseSessionModel> cachedSessionList = new();
    private string nextDSMatchPage = null, nextP2PMatchPage = null;
    private bool isBrowseDSMatchComplete = true, isBrowseP2PComplete = true;

    private bool IsBrowseDSMatchComplete
    {
        get => matchSessionDSModule?.isActive == true ? isBrowseDSMatchComplete : true;
        set => isBrowseDSMatchComplete = matchSessionDSModule?.isActive == true ? value : true;
    }

    private bool IsBrowseP2PComplete
    {
        get => matchSessionP2PModule?.isActive == true ? isBrowseP2PComplete : true;
        set => isBrowseP2PComplete = matchSessionP2PModule?.isActive == true ? value : true;
    }

    private void Awake()
    {
        refreshButton.onClick.AddListener(() => BrowseMatch());
        widgetSwitcher.OnRetryButtonClicked = () => BrowseMatch();
        sessionListScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        matchSessionDSModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionDSEssentials);
        matchSessionP2PModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionP2PEssentials);
        
        BrowseMatch();
    }

    private void OnDisable()
    {
        Reset();
    }

    private void Reset()
    {
        cachedSessionList.Clear();
        sessionListPanel.DestroyAllChildren();

        nextDSMatchPage = nextP2PMatchPage = string.Empty;
        IsBrowseDSMatchComplete = IsBrowseP2PComplete = true;
    }

    private void OnScrollValueChanged(Vector2 scrollPos)
    {
        // On reached bottom, query the next page.
        if (scrollPos.y <= 0)
        {
            BrowseMatch(isLoadNextPage: true);
        }
    }

    private void BrowseMatch(bool isLoadNextPage = false)
    {
        // Abort if last request is still running.
        if (!IsBrowseDSMatchComplete || !IsBrowseP2PComplete)
        {
            return;
        }

        // Abort if no next page needs to be loaded.
        bool hasNextDS = !string.IsNullOrEmpty(nextDSMatchPage);
        bool hasNextP2P = !string.IsNullOrEmpty(nextP2PMatchPage);
        if (isLoadNextPage && !hasNextDS && !hasNextP2P)
        {
            return;
        }

        /* If loading the next page, show the list loader.
         * Otherwise, show the full loading state to indicate the game session list is being refreshed. */
        refreshButton.gameObject.SetActive(false);
        listLoaderPanel.gameObject.SetActive(isLoadNextPage);
        if (!isLoadNextPage)
        {
            Reset();
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);
        }

        // If loading the next page, only invoke the request if the pagination is not null.
        if (isLoadNextPage)
        {
            IsBrowseDSMatchComplete = !hasNextDS;
            IsBrowseP2PComplete = !hasNextP2P;

            if (hasNextDS) OnBrowseDSMatch?.Invoke(nextDSMatchPage, OnBrowseMatchComplete);
            if (hasNextP2P) OnBrowseP2PMatch?.Invoke(nextP2PMatchPage, OnBrowseMatchComplete);
        }
        // Otherwise, invoke both requests.
        else
        {
            IsBrowseDSMatchComplete = IsBrowseP2PComplete = false;
            OnBrowseDSMatch?.Invoke(string.Empty, OnBrowseMatchComplete);
            OnBrowseP2PMatch?.Invoke(string.Empty, OnBrowseMatchComplete);
        }
    }

    private void OnBrowseMatchComplete(Result<BrowseSessionResult> result)
    {
        if (result.IsError)
        {
            refreshButton.gameObject.SetActive(true);
            listLoaderPanel.gameObject.SetActive(false);
            widgetSwitcher.ErrorMessage = result.Error.Message;
            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
            return;
        }

        Dictionary<string, object> queryAttribute = result.Value.QueryAttribute;
        PaginatedResponse<BrowseSessionModel> paginatedResult = result.Value.Result;

        // Store pagination to query the next sessions.
        if (queryAttribute.TryGetValue(MatchSessionAttributeKey, out object value) && 
            value is string matchSessionType)
        {
            if (matchSessionType == MatchSessionDSAttributeValue)
            {
                IsBrowseDSMatchComplete = true;
                nextDSMatchPage = paginatedResult.paging.next;
            }
            else if (matchSessionType == MatchSessionP2PAttributeValue)
            {
                IsBrowseP2PComplete = true;
                nextP2PMatchPage = paginatedResult.paging.next;
            }
        }

        // Generate session entries.
        foreach (BrowseSessionModel model in paginatedResult.data)
        {
            if (cachedSessionList.TryAdd(model.Session.id, model))
            {
                Instantiate(sessionEntryPrefab, sessionListPanel).Setup(model);
            }
        }

        // Display the session list if not empty.
        if (IsBrowseDSMatchComplete && IsBrowseP2PComplete)
        {
            refreshButton.gameObject.SetActive(true);
            listLoaderPanel.gameObject.SetActive(false);

            widgetSwitcher.SetWidgetState(
                cachedSessionList.Count > 0 ?
                AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty :
                AccelByteWarsWidgetSwitcher.WidgetState.Empty);
        }
    }

    public override GameObject GetFirstButton()
    {
        return refreshButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.BrowseMatchMenu;
    }
}
