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
    private MatchSessionServerType selectedSessionServerType = MatchSessionServerType.DedicatedServer;
    private MatchSessionDSWrapper matchSessionDSWrapper;

    // Start is called before the first frame update
    private void Awake()
    {
        instance ??= this;
    }

    private void Start()
    {
        matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
            
        createEliminationBtn.onClick.AddListener(OnCreateEliminationBtnClicked);
        createTeamDeathMatchBtn.onClick.AddListener(OnTeamDeathMatchBtnClicked);
        backBtn.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        dsBtn.onClick.AddListener(OnDSBtnClicked);
        p2pBtn.onClick.AddListener(OnP2PBtnClicked);
        backFromServerTypeBtn.onClick.AddListener(OnBackFromServerTypeBtnClicked);
        selectServerPanel.HideRight();
        GameManager.OnDisconnectedInMainMenu += OnDisconnectedInMainMenu;
    }

    private void OnDisconnectedInMainMenu(string reason)
    {
        ShowError($"disconnected from server, message: {reason}");
    }

    private void OnP2PBtnClicked()
    {
        selectedSessionServerType = MatchSessionServerType.PeerToPeer;
        CreateMatchSession();
    }

    private void OnDSBtnClicked()
    {
        dsBtn.interactable = false;
        selectedSessionServerType = TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials)
                                         ? MatchSessionServerType.DedicatedServerAMS
                                         : MatchSessionServerType.DedicatedServer;
        CreateMatchSession();
    }

    private void CreateMatchSession()
    {
        ShowLoading("Creating Match Session...", CancelCreateMatch);
        matchSessionDSWrapper.Create(gameMode, 
            selectedSessionServerType, OnCreatedMatchSession);
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

    private void ShowLoading(string loadingInfo, UnityAction cancelCallback=null)
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
        loadingPanel.gameObject.SetActive(false);
        errorPanel.Show(errorInfo, HideError);
    }

    private void HideError()
    {
        if(shownRectTransform!=null)
            shownRectTransform.gameObject.SetActive(true);
        errorPanel.gameObject.SetActive(false);
    }

    private void CancelCreateMatch()
    {
        if(shownRectTransform!=null)
            shownRectTransform.gameObject.SetActive(true);
        loadingPanel.gameObject.SetActive(false);
        matchSessionDSWrapper.CancelCreateMatchSession();
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
