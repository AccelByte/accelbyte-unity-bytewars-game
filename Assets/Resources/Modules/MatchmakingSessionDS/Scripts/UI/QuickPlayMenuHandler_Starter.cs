using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;


public class QuickPlayMenuHandler_Starter : MenuCanvas
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
    
    //Copy 3a code from connecting-game-mode-selection-ui-with-matchmaking step 1 here
    
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

    // Start is called before the first frame update
    void Start()
    {
        //Copy 3a code from connecting-game-mode-selection-ui-with-matchmaking step 2 here

        _panels = new List<GameObject>()
        {
            contentPanel, 
            findingMatchPanel, 
            joiningMatchPanel, 
            cancelingMatchPanel, 
            failedPanel
        };
        
        //Copy 3a code from Ready The UI step 2 here

    }

    //Copy 3a code from Ready The UI step 1 here
    //Modify OnEliminationButtonClicked() from connecting-game-mode-selection-ui-with-matchmaking step 3
    //Modify OnCancelMatchmakingClicked()() from connecting-game-mode-selection-ui-with-matchmaking step 5
    

    //Copy 3a code from connecting-game-mode-selection-ui-with-matchmaking step 4
    
    
    //Copy 3a code from connecting-game-mode-selection-ui-with-matchmaking step 6
    
    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.QuickPlayMenuCanvas;
    }
    
    private void OnEnable()
    {
        currentView = QuickPlayView.Default;
    }
}
