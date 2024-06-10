// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateMatchSessionHandler : MenuCanvas
{
    [SerializeField] private Button createEliminationBtn;
    [SerializeField] private Button createTeamDeathMatchBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button dsBtn;
    [SerializeField] private Button p2pBtn;
    [SerializeField] private Button backFromServerTypeBtn;
    
    [SerializeField] private PanelGroup createMatchPanel;
    [SerializeField] private PanelGroup selectServerPanel;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private ErrorPanel errorPanel;
    private static CreateMatchSessionHandler instance = null;
    private RectTransform shownRectTransform;
    private InGameMode gameMode = InGameMode.None;
    private GameSessionServerType selectedSessionServerType = GameSessionServerType.DedicatedServer;
    private MatchSessionDSWrapper matchSessionDSWrapper;
    private MatchSessionP2PWrapper matchSessionP2PWrapper;

    // Start is called before the first frame update
    private void Awake()
    {
        instance ??= this;
    }

    private void Start()
    {
        matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
        matchSessionP2PWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionP2PWrapper>(); 

        createEliminationBtn.onClick.AddListener(OnCreateEliminationBtnClicked);
        createTeamDeathMatchBtn.onClick.AddListener(OnTeamDeathMatchBtnClicked);
        backBtn.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        dsBtn.gameObject.SetActive(matchSessionDSWrapper != null);
        dsBtn.onClick.AddListener(OnDSBtnClicked);
        p2pBtn.gameObject.SetActive(matchSessionP2PWrapper != null);
        p2pBtn.onClick.AddListener(OnP2PBtnClicked);
        backFromServerTypeBtn.onClick.AddListener(OnBackFromServerTypeBtnClicked);
        selectServerPanel.HideRight();
    }

    private void OnP2PBtnClicked()
    {
        selectedSessionServerType = GameSessionServerType.PeerToPeer;
        CreateMatchSession();
    }

    private void OnDSBtnClicked()
    {
        dsBtn.interactable = false;
        selectedSessionServerType = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials)
                                         ? GameSessionServerType.DedicatedServerAMS
                                         : GameSessionServerType.DedicatedServer;
        CreateMatchSession();
    }

    private void CreateMatchSession()
    {
        ShowLoading("Creating Match Session...", CancelCreateMatch);
        switch (selectedSessionServerType)
        {
            case GameSessionServerType.DedicatedServer or GameSessionServerType.DedicatedServerAMS:
                matchSessionDSWrapper.Create(gameMode, 
                    selectedSessionServerType, OnCreatedMatchSession);
                break;
            case GameSessionServerType.PeerToPeer:
                matchSessionP2PWrapper.CreateP2P(gameMode, 
                    selectedSessionServerType, OnCreatedMatchSession);
                break;
        }
    }

    private static void OnCreatedMatchSession(string errorMessage)
    {
        instance.dsBtn.interactable = true;
        if (!String.IsNullOrEmpty(errorMessage))
        {
            instance.ShowError(errorMessage);
        }
        else
        {
            //show loading, MatchSessionWrapper will move UI to lobby when connected to DS
            instance.ShowLoading("Joining Session");
            Debug.Log($"success create session");
        }
    }

    private void OnTeamDeathMatchBtnClicked()
    {
        gameMode = InGameMode.CreateMatchDeathMatchGameMode;
        //show server selection panel
        shownRectTransform = SlideShowLeft(createMatchPanel, selectServerPanel);
    }

    private void OnBackFromServerTypeBtnClicked()
    {
        gameMode = InGameMode.None;
        shownRectTransform = SlideShowRight(selectServerPanel, createMatchPanel);
    }

    private void OnCreateEliminationBtnClicked()
    {
        gameMode = InGameMode.CreateMatchEliminationGameMode;
        //show server selection panel
        shownRectTransform = SlideShowLeft(createMatchPanel, selectServerPanel);
    }

    private void ShowLoading(string loadingInfo, UnityAction cancelCallback=null, bool cancelBtn = true)
    {
        if(shownRectTransform!=null)
            shownRectTransform.gameObject.SetActive(false);
        loadingPanel.Show(loadingInfo, cancelCallback);
        errorPanel.gameObject.SetActive(false);
    }

    private void HideLoadingBackToMainPanel()
    {
        createMatchPanel.gameObject.SetActive(true);
        OnBackFromServerTypeBtnClicked();
        loadingPanel.gameObject.SetActive(false);
    }

    private void ShowError(string errorInfo)
    {
        if (shownRectTransform != null)
        {
            shownRectTransform.gameObject.SetActive(false);
        }

        loadingPanel.gameObject.SetActive(false);
        errorPanel.Show(errorInfo, HideError);
    }

    private void HideError()
    {
        if (shownRectTransform != null)
        {
            shownRectTransform.gameObject.SetActive(true);
        }

        errorPanel.gameObject.SetActive(false);
    }

    private void CancelCreateMatch()
    {
        loadingPanel.gameObject.SetActive(false);
        switch (selectedSessionServerType)
        {
            case GameSessionServerType.DedicatedServerAMS:
                matchSessionDSWrapper.CancelCreateMatchSession();
                break;
            case GameSessionServerType.PeerToPeer:
                matchSessionP2PWrapper.CancelCreateMatchSessionP2P();
                break;
            default:
                break;
        }
    }

    private void CancelCreateMatchP2P()
    {
        loadingPanel.gameObject.SetActive(false);
        matchSessionP2PWrapper.CancelCreateMatchSessionP2P();
    }

    private RectTransform SlideShowLeft(PanelGroup toHide, PanelGroup toShow)
    {
        toHide.HideSlideLeft();
        var rectTransform = toShow.Show();
        return rectTransform;
    }

    private RectTransform SlideShowRight(PanelGroup toHide, PanelGroup toShow)
    {
        toHide.HideSlideRight();
        var rectTransform = toShow.Show();
        return rectTransform;
    }

    private void OnDisable()
    {
        HideError();
        HideLoadingBackToMainPanel();
    }

    public override GameObject GetFirstButton()
    {
        return createEliminationBtn.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchMenuCanvas;
    }
}
