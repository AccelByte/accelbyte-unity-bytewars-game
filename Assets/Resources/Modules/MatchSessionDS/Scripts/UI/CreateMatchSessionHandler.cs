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
    private static CreateMatchSessionHandler _instance = null;
    private RectTransform _shownRectTransform;
    private InGameMode _gameMode = InGameMode.None;
    private MatchSessionServerType _selectedSessionServerType = MatchSessionServerType.DedicatedServer;

    private MatchSessionDSWrapper _matchSessionDSWrapper;
    // Start is called before the first frame update
    private void Awake()
    {
        _instance ??= this;
    }

    private void Start()
    {
        _matchSessionDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchSessionDSWrapper>();
            
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
        _selectedSessionServerType = MatchSessionServerType.PeerToPeer;
        CreateMatchSession();
    }

    private void OnDSBtnClicked()
    {
        dsBtn.interactable = false;
        _selectedSessionServerType = MatchSessionServerType.DedicatedServer;
        CreateMatchSession();
    }

    private void CreateMatchSession()
    {
        ShowLoading("Creating Match Session...", CancelCreateMatch);
        _matchSessionDSWrapper.Create(_gameMode, 
            _selectedSessionServerType, OnCreatedMatchSession);
    }

    private static void OnCreatedMatchSession(string errorMessage)
    {
        _instance.dsBtn.interactable = true;
        if (!String.IsNullOrEmpty(errorMessage))
        {
            _instance.ShowError(errorMessage);
        }
        else
        {
            //show loading, MatchSessionWrapper will move UI to lobby when connected to DS
            _instance.ShowLoading("Joining Session");
            Debug.Log($"success create session");
        }
    }

    private void OnTeamDeathMatchBtnClicked()
    {
        _gameMode = InGameMode.CreateMatchDeathMatchGameMode;
        //show server selection panel
        _shownRectTransform = SlideShowLeft(createMatchPanel, selectServerPanel);
    }

    private void OnBackFromServerTypeBtnClicked()
    {
        _gameMode = InGameMode.None;
        _shownRectTransform = SlideShowRight(selectServerPanel, createMatchPanel);
    }

    private void OnCreateEliminationBtnClicked()
    {
        _gameMode = InGameMode.CreateMatchEliminationGameMode;
        //show server selection panel
        _shownRectTransform = SlideShowLeft(createMatchPanel, selectServerPanel);
    }

    private void ShowLoading(string loadingInfo, UnityAction cancelCallback=null)
    {
        if(_shownRectTransform!=null)
            _shownRectTransform.gameObject.SetActive(false);
        loadingPanel.Show(loadingInfo, cancelCallback);
        errorPanel.gameObject.SetActive(false);
    }

    private void ShowError(string errorInfo)
    {
        loadingPanel.gameObject.SetActive(false);
        errorPanel.Show(errorInfo, HideError);
    }

    private void HideError()
    {
        if(_shownRectTransform!=null)
            _shownRectTransform.gameObject.SetActive(true);
        errorPanel.gameObject.SetActive(false);
    }

    private void CancelCreateMatch()
    {
        if(_shownRectTransform!=null)
            _shownRectTransform.gameObject.SetActive(true);
        loadingPanel.gameObject.SetActive(false);
        _matchSessionDSWrapper.CancelCreateMatchSession();
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
