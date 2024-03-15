using System;
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
    
    private RectTransform shownRectTransform;
    private InGameMode gameMode = InGameMode.None;
    private MatchSessionServerType selectedSessionServerType = MatchSessionServerType.DedicatedServer;
    
    private static CreateMatchSessionHandler_Starter instance = null;
    private const string ClassName = "[CreateMatchSessionHandler_Starter]";
    
    //Copy MatchSessionDSWrapper_Starter here

    private void Awake()
    {
        instance ??= this;
    }

    private void Start()
    {

        //Copy MatchSessionDSWrapper_Starter for start() here

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
        gameMode = InGameMode.None;
        shownRectTransform = SlideShowRight(selectServerPanel, createMatchPanel);
    }

    private void OnDSBtnClicked()
    {
        // Copy OnDSBtnClicked code here
    }

    private void OnTeamDeathMatchBtnClicked()
    {
        gameMode = InGameMode.CreateMatchDeathMatchGameMode;
        //show server selection panel
        shownRectTransform = SlideShowLeft(createMatchPanel, selectServerPanel);
    }

    private void OnCreateEliminationBtnClicked()
    {
        // Copy OnCreateEliminationBtnClicked code here

    }
    #endregion ButtonAction
    
    private void CreateMatchSession()
    {
        Debug.Log($"{ClassName} Create Match Session not yet implemented"); // delete this after copy CreateMatchSession implementation
        // Copy CreateMatchSession code here
    }

    // Copy OnCreatedMatchSession function here

    #region UI

    // Copy CancelCreateMatch code here

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
        if(shownRectTransform!=null)
            shownRectTransform.gameObject.SetActive(true);
        errorPanel.gameObject.SetActive(false);
    }

    private void ShowLoading(string loadingInfo, UnityAction cancelCallback=null)
    {
        if(shownRectTransform!=null)
            shownRectTransform.gameObject.SetActive(false);
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
