using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateMatchSessionHandler_Starter : MenuCanvas
{
    [SerializeField] private Button createEliminationBtn;
    [SerializeField] private Button createTeamDeathMatchBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button dsBtn;
    [SerializeField] private Button backFromServerTypeBtn;
    
    [SerializeField] private PanelGroup createMatchPanel;
    [SerializeField] private PanelGroup selectServerPanel;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private ErrorPanel errorPanel;
    
    private RectTransform _shownRectTransform;
    private InGameMode _gameMode = InGameMode.None;
    private MatchSessionServerType _selectedSessionServerType = MatchSessionServerType.DedicatedServer;
    
    private static CreateMatchSessionHandler_Starter _instance = null;
    private const string ClassName = "[CreateMatchSessionHandler_Starter]";
    private void Awake()
    {
        _instance ??= this;
    }
    private void Start()
    {
        createEliminationBtn.onClick.AddListener(OnCreateEliminationBtnClicked);
        createTeamDeathMatchBtn.onClick.AddListener(OnTeamDeathMatchBtnClicked);
        backBtn.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        dsBtn.onClick.AddListener(OnDSBtnClicked);
        backFromServerTypeBtn.onClick.AddListener(OnBackFromServerTypeBtnClicked);
        selectServerPanel.HideRight();
    }
    
    #region ButtonAction
    private void OnBackFromServerTypeBtnClicked()
    {
        _gameMode = InGameMode.None;
        _shownRectTransform = SlideShowRight(selectServerPanel, createMatchPanel);
    }

    private void OnDSBtnClicked()
    {
        
    }

    private void OnTeamDeathMatchBtnClicked()
    {
        
    }

    private void OnCreateEliminationBtnClicked()
    {
        
    }
    #endregion ButtonAction
    
    private void CreateMatchSession()
    {
        //we will call Accelbyte SDK API via wrapper class in this method
        Debug.Log($"{ClassName} Create Match Session not yet implemented");
    }

    #region UI
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
    private void HideError()
    {
        if(_shownRectTransform!=null)
            _shownRectTransform.gameObject.SetActive(true);
        errorPanel.gameObject.SetActive(false);
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
    private void OnDisable()
    {
        HideError();
    }
    #endregion UI
    
    #region MenuCanvasOverride
    public override GameObject GetFirstButton()
    {
        return createEliminationBtn.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchMenuCanvas;
    }
    #endregion MenuCanvasOverride
}
